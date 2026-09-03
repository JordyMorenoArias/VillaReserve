# Spec: 001 - Domain Model and Persistence

## Objective

Define and implement the initial VillaReserve domain model and its PostgreSQL persistence model using Entity Framework Core.

This specification establishes the business entities, relationships, persistence constraints, enumerations, indexes, and database mappings required by the subsequent VillaReserve features.

The implementation must use Entity Framework Core Fluent API to explicitly configure persistence rules where appropriate.

This specification does not implement complete business workflows or API endpoints.

---

# Context

VillaReserve is a reservation management system for a single villa.

The system must eventually support:

- Administrator management
- Reservation requests
- Reservation confirmation and cancellation
- Availability calculation
- Manual blocked periods
- Notifications
- Google Calendar synchronization
- Secure reservation access
- Audit logging

The persistence model must be designed to support these capabilities while keeping the database as the system of record.

Availability must not be represented by a dedicated `availability` table or an `is_available` boolean.

Availability will be calculated from active reservations, blocked periods, and relevant external calendar events.

---

# Scope

## In Scope

This specification includes:

- Domain entities
- Entity relationships
- Entity identifiers
- Entity properties
- Entity enumerations
- EF Core Fluent API mappings
- PostgreSQL column types
- Primary keys
- Foreign keys
- Unique constraints
- Check constraints
- Indexes
- Reservation overlap protection
- PostgreSQL exclusion constraint
- Required PostgreSQL extensions
- Delete behaviors
- Initial domain migration
- Persistence tests

## Out of Scope

This specification must not implement:

- Authentication flows
- JWT
- Login endpoints
- Password reset
- Reservation creation API
- Reservation confirmation API
- Reservation cancellation workflows
- Availability API
- Admin dashboard
- Email sending
- WhatsApp integration
- Google Calendar synchronization logic
- Notification delivery
- Background jobs
- Frontend functionality

The entities required by these future features may be created here, but their application workflows belong to their respective specifications.

---

# Domain Model

The initial persistence model consists of:

```text
User
Reservation
BlockedPeriod
Notification
CalendarEvent
AuditLog
ReservationToken
```

Relationships:

```text
User
 ├── 1:N BlockedPeriod
 ├── 1:N Notification
 └── 1:N AuditLog

Reservation
 ├── 1:0..1 CalendarEvent
 ├── 1:N ReservationToken
 └── N:1 User (through administrative actions where applicable)
```

Customer accounts are not part of the model.

Customers do not need to authenticate to submit reservation requests.

Customer information is stored directly on the `Reservation` entity.

---

# Entity Requirements

## RT-001 - User

The `User` entity represents an administrator who can manage VillaReserve.

Properties:

```text
Id
Email
PasswordHash
FirstName
LastName
IsActive
CreatedAt
UpdatedAt
```

Requirements:

- `Id` must be a UUID/GUID.
- `Email` is required.
- `Email` must be unique.
- `Email` should have an appropriate maximum length.
- `PasswordHash` is required.
- `FirstName` is required.
- `LastName` is required.
- `IsActive` is required.
- `CreatedAt` is required.
- `UpdatedAt` is required.

Authentication behavior is out of scope.

---

## RT-002 - Reservation

The `Reservation` entity represents a customer reservation or reservation request.

Properties:

```text
Id
CustomerName
CustomerPhone
CustomerEmail
GuestCount
StartDateTime
EndDateTime
Status
Notes
CreatedAt
UpdatedAt
ConfirmedAt
CancelledAt
```

Requirements:

- `Id` must be a UUID/GUID.
- `CustomerName` is required.
- `CustomerPhone` is required.
- `CustomerEmail` is optional.
- `GuestCount` is optional because no maximum capacity has been established.
- `StartDateTime` is required.
- `EndDateTime` is required.
- `Status` is required.
- `Notes` is optional.
- `CreatedAt` is required.
- `UpdatedAt` is required.
- `ConfirmedAt` is optional.
- `CancelledAt` is optional.

The following reservation statuses must exist:

```text
PENDING
CONFIRMED
REJECTED
CANCELLED
EXPIRED
```

---

## RT-003 - Reservation Date Validation

The database must guarantee:

```text
EndDateTime > StartDateTime
```

The persistence model must therefore include a PostgreSQL check constraint equivalent to:

```sql
CHECK (end_datetime > start_datetime)
```

---

## RT-004 - Guest Count Validation

If `GuestCount` is provided, it must be greater than zero.

The database must enforce:

```text
GuestCount IS NULL OR GuestCount > 0
```

No maximum guest count must be enforced because the business has not defined one.

---

## RT-005 - Reservation Status Constraints

A reservation with status `CONFIRMED` must have a `ConfirmedAt` value.

A reservation with status `CANCELLED` must have a `CancelledAt` value.

The persistence model should enforce these invariants through PostgreSQL check constraints where practical.

---

## RT-006 - Reservation Overlap Prevention

Active reservations must not overlap.

The following statuses are considered blocking:

```text
PENDING
CONFIRMED
```

The following statuses do not block availability:

```text
REJECTED
CANCELLED
EXPIRED
```

PostgreSQL must enforce this rule using an exclusion constraint.

The conceptual constraint is:

```sql
EXCLUDE USING gist (
    tstzrange(start_datetime, end_datetime, '[)') WITH &&
)
WHERE (status IN ('PENDING', 'CONFIRMED'))
```

The `[)` interval definition means:

- Start is inclusive.
- End is exclusive.

Therefore:

```text
10:00 - 15:00
15:00 - 18:00
```

are valid adjacent intervals.

But:

```text
10:00 - 15:00
14:00 - 18:00
```

must not be allowed.

The application should perform an availability pre-check for user-friendly errors, but PostgreSQL must remain the final concurrency protection.

---

## RT-007 - Date and Time Persistence

Reservation date/time values must use PostgreSQL:

```text
TIMESTAMPTZ
```

The C# model must use timezone-aware date/time representations appropriate for the application's chosen strategy.

The official VillaReserve business timezone must be defined before reservation workflows are implemented.

The persistence layer must not silently convert business dates to server-local time.

---

## RT-008 - BlockedPeriod

`BlockedPeriod` represents a manually configured period during which the villa cannot be reserved.

Properties:

```text
Id
StartDateTime
EndDateTime
Reason
CreatedBy
CreatedAt
UpdatedAt
```

Requirements:

- `Id` is a UUID/GUID.
- `StartDateTime` is required.
- `EndDateTime` is required.
- `Reason` is required.
- `CreatedBy` is required.
- `CreatedAt` is required.
- `UpdatedAt` is required.
- `CreatedBy` references `User.Id`.

The database must enforce:

```text
EndDateTime > StartDateTime
```

Blocked periods must be represented independently from reservations.

---

## RT-009 - Notification

`Notification` represents an in-application notification for an administrator.

Properties:

```text
Id
UserId
Type
Title
Message
IsRead
ReadAt
CreatedAt
```

Requirements:

- `Id` is a UUID/GUID.
- `UserId` is required.
- `Type` is required.
- `Title` is required.
- `Message` is required.
- `IsRead` is required.
- `ReadAt` is optional.
- `CreatedAt` is required.
- `UserId` references `User.Id`.

If `IsRead` is `false`, `ReadAt` should be null.

If `IsRead` is `true`, `ReadAt` should contain the time at which the notification was read.

---

## RT-010 - CalendarEvent

`CalendarEvent` represents the relationship between a VillaReserve reservation and an external calendar event.

Properties:

```text
Id
ReservationId
Provider
ExternalEventId
SyncStatus
LastSyncedAt
CreatedAt
UpdatedAt
```

Requirements:

- `Id` is a UUID/GUID.
- `ReservationId` is required.
- `Provider` is required.
- `ExternalEventId` is required.
- `SyncStatus` is required.
- `LastSyncedAt` is optional.
- `CreatedAt` is required.
- `UpdatedAt` is required.
- `ReservationId` references `Reservation.Id`.

A reservation may have zero or one calendar integration record.

Therefore:

```text
Reservation 1 ─── 0..1 CalendarEvent
```

The provider must initially support:

```text
GOOGLE
```

The synchronization status must support:

```text
PENDING
SYNCED
FAILED
```

The actual Google Calendar integration is out of scope.

---

## RT-011 - AuditLog

`AuditLog` represents an immutable record of important system changes.

Properties:

```text
Id
UserId
Action
EntityType
EntityId
OldValue
NewValue
CreatedAt
```

Requirements:

- `Id` is a UUID/GUID.
- `UserId` is optional.
- `Action` is required.
- `EntityType` is required.
- `EntityId` is required.
- `OldValue` is optional.
- `NewValue` is optional.
- `CreatedAt` is required.

`OldValue` and `NewValue` should use PostgreSQL `JSONB`.

The `User` relationship should use a nullable foreign key.

If a user is deleted in the future, the audit record must not be deleted automatically.

Audit logs must not be physically modified as part of normal application behavior.

---

## RT-012 - ReservationToken

`ReservationToken` represents a secure token used to provide controlled access to reservation-related operations.

Properties:

```text
Id
ReservationId
TokenHash
Purpose
ExpiresAt
UsedAt
CreatedAt
```

Requirements:

- `Id` is a UUID/GUID.
- `ReservationId` is required.
- `TokenHash` is required.
- `Purpose` is required.
- `ExpiresAt` is required.
- `UsedAt` is optional.
- `CreatedAt` is required.
- `ReservationId` references `Reservation.Id`.

Supported purposes:

```text
ADMIN_ACCESS
CUSTOMER_ACCESS
```

The raw token must never be persisted.

Only a cryptographic hash of the token may be stored.

Token generation and validation workflows are out of scope.

---

# Entity Relationships

## RT-013 - User Relationships

The following relationships must exist:

```text
User 1 ─── N BlockedPeriod
User 1 ─── N Notification
User 1 ─── N AuditLog
```

Foreign keys:

```text
BlockedPeriod.CreatedBy → User.Id
Notification.UserId    → User.Id
AuditLog.UserId        → User.Id
```

---

## RT-014 - Reservation Relationships

The following relationships must exist:

```text
Reservation 1 ─── 0..1 CalendarEvent
Reservation 1 ─── N ReservationToken
```

Foreign keys:

```text
CalendarEvent.ReservationId
ReservationToken.ReservationId
```

A reservation must not be physically deleted if dependent business records would lose important historical information.

---

# EF Core Fluent API

## RT-015 - Explicit Persistence Configuration

Persistence rules must be configured through EF Core Fluent API.

Entity configurations should be organized using:

```text
IEntityTypeConfiguration<T>
```

when appropriate.

The persistence structure should follow:

```text
Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    └── Configurations/
        ├── UserConfiguration.cs
        ├── ReservationConfiguration.cs
        ├── BlockedPeriodConfiguration.cs
        ├── NotificationConfiguration.cs
        ├── CalendarEventConfiguration.cs
        ├── AuditLogConfiguration.cs
        └── ReservationTokenConfiguration.cs
```

The exact location may be adjusted if consistent with the project's architecture.

---

## RT-016 - Table Naming

Database table names should use a consistent naming convention.

The recommended names are:

```text
users
reservations
blocked_periods
notifications
calendar_events
audit_logs
reservation_tokens
```

Column names should follow the project's PostgreSQL naming convention consistently.

---

## RT-017 - UUID Configuration

All primary keys must use UUID-compatible database columns.

The application must not rely on integer identity columns.

---

## RT-018 - String Configuration

String properties must have explicit maximum lengths where business requirements permit.

Unbounded text must only be used where appropriate, such as:

- Notes
- Audit JSON
- Notification message content when justified

The Fluent API must not blindly configure every string as unlimited text.

---

## RT-019 - Enum Persistence

Application enums must not rely on implicit integer persistence where database readability and safety would benefit from explicit values.

The chosen persistence strategy must ensure that adding or reordering enum members cannot silently change the meaning of existing database records.

String-based enum persistence is preferred for the initial domain model unless there is a justified reason to use PostgreSQL native enums.

---

# Database Constraints and Indexes

## RT-020 - Unique Constraints

The following values must be unique:

```text
users.email
```

The following combination must also be unique:

```text
calendar_events.provider
calendar_events.external_event_id
```

`reservation_tokens.token_hash` must be unique.

---

## RT-021 - Foreign Key Delete Behavior

Delete behavior must be explicitly configured.

Recommended behavior:

```text
User → BlockedPeriod
    Restrict / NoAction

User → Notification
    Cascade or controlled deletion depending on lifecycle

User → AuditLog
    SetNull

Reservation → CalendarEvent
    Cascade or controlled deletion

Reservation → ReservationToken
    Cascade or controlled deletion
```

Important historical records must not be accidentally deleted through cascading relationships.

The implementation must choose and document the final behavior for each relationship.

---

## RT-022 - Indexes

Indexes must be created for frequently queried foreign keys and operational queries.

At minimum, consider indexes for:

```text
users.email
blocked_periods.created_by
notifications.user_id
notifications.is_read
calendar_events.reservation_id
calendar_events.provider + external_event_id
audit_logs.user_id
audit_logs.entity_type + entity_id
reservation_tokens.reservation_id
reservation_tokens.token_hash
```

Indexes should be added based on actual query patterns and not indiscriminately.

---

# PostgreSQL Extensions

## RT-023 - Required Extensions

The migration must ensure the following PostgreSQL extensions exist:

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS btree_gist;
```

These extensions must be managed by EF Core migrations.

---

# Migration

## RT-024 - Initial Domain Migration

A new EF Core migration must create the domain persistence model.

The migration must create:

```text
users
reservations
blocked_periods
notifications
calendar_events
audit_logs
reservation_tokens
```

It must also create:

- Primary keys
- Foreign keys
- Unique constraints
- Check constraints
- Required indexes
- Reservation overlap exclusion constraint
- Required PostgreSQL extensions

---

# Availability Model

## RT-025 - No Availability Table

The implementation must not create:

```text
availability
```

as a persistent entity/table.

The implementation must not create:

```text
is_available
```

on the villa or reservation model.

Availability is derived from:

```text
Active Reservations
+
Blocked Periods
+
Relevant Calendar Events
```

The availability calculation itself belongs to a future specification.

---

# Reservation Blocking Model

## RT-026 - Blocking Statuses

The database overlap constraint must only consider:

```text
PENDING
CONFIRMED
```

as blocking reservation statuses.

Therefore:

```text
PENDING    → blocks
CONFIRMED  → blocks
REJECTED   → does not block
CANCELLED  → does not block
EXPIRED    → does not block
```

This rule must be reflected consistently between the database constraint and future application logic.

---

# Persistence Testing

## RT-027 - Entity Mapping Tests

The persistence layer must contain tests that verify the model can be created and persisted correctly.

Tests should verify:

- Entity relationships
- Required fields
- Foreign keys
- Unique constraints
- Check constraints
- Date/time persistence
- Enum persistence

---

## RT-028 - Reservation Overlap Integration Test

An integration test using PostgreSQL must verify that two active reservations cannot overlap.

Example:

```text
Reservation A
10:00 ───────── 15:00

Reservation B
14:00 ───────── 18:00
```

The second reservation must fail.

---

## RT-029 - Adjacent Reservation Test

An integration test must verify that adjacent reservations are allowed.

Example:

```text
Reservation A
10:00 ───────── 15:00

Reservation B
15:00 ───────── 18:00
```

Both reservations must be accepted.

---

## RT-030 - Non-Blocking Status Test

An integration test must verify that a reservation with one of the following statuses does not prevent a new reservation:

```text
REJECTED
CANCELLED
EXPIRED
```

---

# Technical Acceptance Criteria

## CA-001 - Build

Running:

```bash
dotnet build
```

must complete successfully with:

```text
0 errors
0 warnings
```

---

## CA-002 - Migration

Running:

```bash
dotnet ef database update
```

must successfully create the complete domain persistence model.

---

## CA-003 - Tables

The PostgreSQL database must contain:

```text
users
reservations
blocked_periods
notifications
calendar_events
audit_logs
reservation_tokens
```

---

## CA-004 - Relationships

All required foreign keys must exist and enforce referential integrity.

---

## CA-005 - Constraints

The database must enforce:

- Unique user email
- Unique calendar external event
- Unique reservation token hash
- Valid reservation intervals
- Valid blocked intervals
- Valid guest counts
- Required timestamps for confirmed/cancelled reservations
- No overlap between active reservations

---

## CA-006 - Reservation Overlap

Two `PENDING` or `CONFIRMED` reservations with overlapping intervals must not be persisted.

---

## CA-007 - Adjacent Intervals

Two reservations where:

```text
Reservation A.EndDateTime == Reservation B.StartDateTime
```

must be allowed.

---

## CA-008 - Inactive Reservation Statuses

Reservations with:

```text
REJECTED
CANCELLED
EXPIRED
```

must not participate in the active reservation overlap constraint.

---

## CA-009 - PostgreSQL Extensions

The database must contain:

```text
pgcrypto
btree_gist
```

after applying the migration.

---

## CA-010 - Tests

Running:

```bash
dotnet test
```

must complete successfully.

The integration test suite must verify the PostgreSQL-specific reservation overlap behavior.

---

# Expected Result

At the end of this specification, VillaReserve must have a complete initial persistence model representing the core business entities.

The backend must have:

- Domain entities
- EF Core mappings
- PostgreSQL tables
- Relationships
- Foreign keys
- Unique constraints
- Check constraints
- Indexes
- Reservation overlap protection
- PostgreSQL extensions
- Initial migration
- Persistence integration tests

No complete business workflows or API endpoints are required.

The resulting model must provide a stable persistence foundation for subsequent specifications.

---

# Future Specifications

The expected implementation order is:

```text
000 - Backend Technical Foundation
001 - Domain Model and Persistence
002 - Authentication
003 - Reservations
004 - Availability
005 - Blocked Periods
006 - Notifications
007 - Google Calendar
008 - Reservation Access Tokens
009 - Administration
```

Future specifications must build upon the entities and persistence rules established here rather than redefining them.
