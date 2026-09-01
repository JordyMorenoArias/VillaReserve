# VillaReserve.Api — Backend Agent Instructions

## 1. Project Overview & Scope

`VillaReserve.Api` is the ASP.NET Core backend for the VillaReserve reservation management system.

The backend serves as the authoritative source of truth for:
- Villa reservations and their lifecycle (`PENDING`, `CONFIRMED`, `REJECTED`, `CANCELLED`, `EXPIRED`).
- Authoritative availability calculations and conflict prevention.
- Manually blocked periods (maintenance, personal use, external bookings).
- External calendar synchronization (Google Calendar).
- Administrative authentication and granular authorization.
- Customer and administrator notifications.

---

## 2. Technology Stack & Baseline Standards

The backend must adhere strictly to the following technical foundation:

- **Target Framework**: .NET 9 (`net9.0`)
- **Language Version**: C# 13
- **Web Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Database**: PostgreSQL with `pgcrypto` and `btree_gist` extensions
- **Validation**: FluentValidation
- **Documentation**: OpenAPI / Swagger / Scalar (exposed via `/docs`)
- **Health Checks**: ASP.NET Core Health Checks (exposed via `/health`, verifying app and database connectivity)
- **Testing**: xUnit, FluentAssertions, Moq/NSubstitute, Testcontainers for PostgreSQL

### Strict Compiler & Code Quality Configuration
Every `.csproj` must enforce:
```xml
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<ImplicitUsings>enable</ImplicitUsings>
```
- **Zero Warnings**: The solution must build with 0 compilation errors and 0 warnings.
- **Async & Cancellation**: Every asynchronous method must accept and propagate a `CancellationToken`.

---

## 3. Architecture & Project Organization

The backend follows a **Modular, Feature-Oriented Clean Architecture** that enforces the **Dependency Inversion Principle (DIP)**.

```text
VillaReserve.Api/
├── src/
│   ├── API/                       # Host, Controllers, Middlewares, Program.cs, Filters
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Extensions/
│   │
│   ├── Features/                  # Feature-oriented vertical slices (Domain & Application logic)
│   │   ├── Reservations/
│   │   ├── Availability/
│   │   ├── BlockedPeriods/
│   │   ├── Authentication/
│   │   ├── Notifications/
│   │   └── Calendar/
│   │
│   ├── Infrastructure/            # Persistence, External APIs, EF Core DbContext, Configurations
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   └── External/
│   │
│   └── Shared/                    # Cross-cutting primitives (Errors, Authorization, Common Extensions)
│       ├── Authorization/
│       ├── Errors/
│       └── Validation/
│
├── tests/
│   ├── Unit/                      # Fast in-memory unit tests
│   └── Integration/               # Real PostgreSQL integration tests (Testcontainers)
│
├── Dockerfile
├── docker-compose.yml
└── VillaReserve.Api.sln
```

### Feature-Oriented Organization Rules
- Keep feature-specific classes (DTOs, Commands/Queries, Handlers, Validators, Domain Logic, and specific Repository Interfaces) inside their respective `Features/<FeatureName>/` folder.
- **Do not create global catch-all folders** like `Services/`, `Repositories/`, `DTOs/`, or `Validators/` when those classes belong to a specific feature.
- `Shared/` is strictly reserved for truly cross-cutting infrastructure (e.g., base `Result` types, global error definitions, common extension methods).

---

## 4. Layering Rules & Dependency Inversion Principle (DIP)

Dependencies must always flow inward toward the domain logic:

```text
HTTP Request
     ↓
Controller / Endpoint (API Layer)
     ↓
Feature Handler / Application Service (Application Layer)
     ↓
Domain Model & Business Rules (Domain Layer)
     ↓
Infrastructure & Repositories (Infrastructure Layer via Abstractions)
     ↓
PostgreSQL / Google Calendar / External Services
```

### Key Architectural Constraints
1. **Thin Controllers**:
   - Controllers must only accept DTOs, delegate execution to feature handlers/services, and return standardized HTTP responses.
   - Controllers must **never** contain business rules, database queries (`DbContext`), complex validation logic, or direct calls to external SDKs.
2. **Dependency Inversion via Interfaces**:
   - Feature services and handlers must depend on abstractions/interfaces (e.g., `IReservationRepository`, `IAvailabilityService`, `ICalendarService`, `IEmailService`, `IUnitOfWork`), **never on concrete infrastructure classes**.
   - Interfaces are defined in the Application/Feature layer; implementations live in `Infrastructure/`.
   - Avoid creating vacuous 1:1 interfaces with no architectural or testing benefit, but always abstract I/O, external integrations, and persistence boundaries.
3. **DTOs & Encapsulation**:
   - EF Core entities must **never** be accepted as controller parameters or returned directly in API responses. Always use explicit Request/Response DTOs.
   - Do not leak raw `IQueryable<T>` out of repository boundaries if it bypasses business invariants or leaks EF Core tracking details.
4. **Service Registration**:
   - Encapsulate service and infrastructure registrations in dedicated extension methods (e.g., `builder.Services.AddInfrastructure(builder.Configuration)`).

---

## 5. Security & ASP.NET Core Authorization System

Security is a primary design requirement. The system uses ASP.NET Core's native authorization capabilities with role-based and policy-based controls.

### 1. Role-Based Authorization
- Used for coarse-grained administrative access:
  ```csharp
  [Authorize(Roles = AppRoles.Admin)]
  ```
- All role names must be declared in strongly-typed constants (e.g., `AppRoles.Admin`), never raw magic strings.

### 2. Policy-Based Authorization
- Complex business permissions must be expressed as named policies registered during startup:
  ```csharp
  builder.Services.AddAuthorization(options =>
  {
      options.AddPolicy(AppPolicies.CanManageReservations, policy =>
          policy.RequireRole(AppRoles.Admin, AppRoles.Manager));
      
      options.AddPolicy(AppPolicies.CanConfirmReservations, policy =>
          policy.Requirements.Add(new MustHaveActiveAdminStatusRequirement()));
  });
  ```

### 3. Custom Requirements & Authorization Handlers
- Use `IAuthorizationRequirement` and `AuthorizationHandler<TRequirement>` (or resource-based `AuthorizationHandler<TRequirement, TResource>`) when authorization depends on dynamic conditions, contextual state, or resource ownership:
  ```csharp
  public class MustBePendingReservationRequirement : IAuthorizationRequirement { }

  public class MustBePendingReservationHandler : AuthorizationHandler<MustBePendingReservationRequirement, Reservation>
  {
      protected override Task HandleRequirementAsync(
          AuthorizationHandlerContext context,
          MustBePendingReservationRequirement requirement,
          Reservation resource)
      {
          if (resource.Status == ReservationStatus.Pending)
          {
              context.Succeed(requirement);
          }
          return Task.CompletedTask;
      }
  }
  ```

### 4. Public Endpoints & Anti-Abuse
- Public endpoints (availability checks, reservation requests) must:
  - Validate and sanitize all inputs via FluentValidation before executing domain logic.
  - Implement rate-limiting to prevent scraping and denial-of-service.
  - Never expose internal database identifiers, server stack traces, or administrative metadata.

---

## 6. Persistence & Entity Framework Core Guidelines

- **Database Engine**: PostgreSQL with `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Database Context**: Single `AppDbContext` in `Infrastructure/Persistence/AppDbContext.cs`.
- **Entity Configurations**: Use dedicated `IEntityTypeConfiguration<T>` classes in `Infrastructure/Persistence/Configurations/`.
- **Identifier Strategy**: Use `Guid` (PostgreSQL UUID) for all primary keys and external resource references.
- **PostgreSQL Extensions**:
  - `pgcrypto`: For database-level cryptographic UUID generation.
  - `btree_gist`: For exclusion constraints preventing overlapping reservation intervals.
  - Extensions must be activated in EF Core migrations via `modelBuilder.HasPostgresExtension("pgcrypto")` and `modelBuilder.HasPostgresExtension("btree_gist")`.
- **Migrations**: Database schema must be managed exclusively through EF Core migrations (`dotnet ef migrations add`, `dotnet ef database update`). Never rely on manual production SQL scripts.
- **Concurrency Control**: Enforce optimistic concurrency on reservations and blocked periods (e.g., using `RowVersion` or concurrency tokens) to prevent double-booking race conditions.

---

## 7. Date & Timezone Standards

- **PostgreSQL Column Type**: Persist business timestamps strictly as `TIMESTAMPTZ` (`timestamp with time zone`).
- **C# Types**: Use `DateTimeOffset` or UTC `DateTime` for all business dates and times (`StartDateTime`, `EndDateTime`).
- **Explicit Timezone Conversion**: Never rely on the host server's local timezone. Store and compute in UTC, and convert to the configured property timezone when evaluating local business rules (e.g., check-in/check-out hours).
- **Interval Overlap Formula**: Overlap between two half-open intervals $[Start_A, End_A)$ and $[Start_B, End_B)$ must strictly follow:
  $$\text{Overlap} \iff (Start_A < End_B) \land (End_A > Start_B)$$

---

## 8. Error Handling & Validation Standards

- **Global Exception Handling**:
  - Centralized exception handling middleware / `IExceptionHandler` mapping exceptions to RFC 7807 `ProblemDetails`.
  - Unhandled errors must return `500 Internal Server Error` with a generic message and correlation ID.
  - **Never leak** connection strings, stack traces, database schema details, or third-party API keys in HTTP responses.
- **Request Validation**:
  - All incoming commands and requests must be validated using FluentValidation before reaching domain logic.
  - Validation failures must return `400 Bad Request` with `ValidationProblemDetails`.
- **Result Pattern / Domain Errors**:
  - Prefer explicit domain error objects or structured Result types for anticipated business rule failures (e.g., `SlotUnavailableError`, `InvalidStateTransitionError`).

---

## 9. Configuration & Secrets Management

- **Options Pattern**: Use strongly-typed configuration classes (`IOptions<T>`) registered with `ValidateDataAnnotations()` and `ValidateOnStart()` to fail fast on application startup if required settings are missing.
- **Secrets**:
  - Never commit credentials, passwords, API keys, or connection strings into source control.
  - Maintain a `.env.example` with dummy values for development setup.
  - Local development uses environment variables or Docker Compose environment files.

---

## 10. External Integrations (Calendar & Notifications)

- **Decoupled Integrations**:
  - Google Calendar and Notification dispatching (Email, WhatsApp link generators) must be abstracted behind interfaces (`ICalendarService`, `IEmailNotificationService`).
  - Core database transactions must not fail or hang if an external notification service is slow or temporarily unavailable. Dispatch notifications via background jobs, outbox pattern, or domain events.
- **Google Calendar Role**:
  - Google Calendar is an external integration and conflict source, **never the primary database**.
  - Synchronize only `CONFIRMED` reservations to Google Calendar.

---

## 11. Automated Testing Standards

All tests must be automated and executable via `dotnet test`.

```text
tests/
├── Unit/               # Tests for domain logic, validation rules, handlers, overlap math
└── Integration/        # API and database tests running against real PostgreSQL
```

- **Unit Tests**:
  - Test domain state transitions, interval conflict algorithms, and validators in isolation.
  - Fast execution with zero external I/O dependencies.
- **Integration Tests**:
  - **Must run against real PostgreSQL** (using Testcontainers or a test PostgreSQL instance).
  - **Strict Prohibition**: SQLite must **not** be used as a test substitute for PostgreSQL, because critical database features (`TIMESTAMPTZ`, UUIDs, `btree_gist`, exclusion constraints) are PostgreSQL-specific.
  - Verify database migrations, repository implementations, health check endpoints (`/health`), and full API endpoint pipelines.
