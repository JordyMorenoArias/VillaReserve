# VillaReserve.Api — Backend Agent Instructions

## 1. Project Overview & Scope

`VillaReserve.Api` is the ASP.NET Core backend for the VillaReserve reservation management system.

The backend serves as the authoritative source of truth for:
- Villa reservations and their lifecycle.
- Availability calculations and conflicts.
- Manually blocked periods.
- External calendar synchronization (e.g., Google Calendar).
- Administrative authentication and authorization.
- Notification dispatching.

---

## 2. Core Architectural Principles

- **Separation of Concerns**: Maintain a clean separation between Domain, Application (Use Cases / Services), Infrastructure (Data access, external APIs), and API (Controllers / Endpoints).
- **Domain-Driven Design**: Centralize core business and availability rules within the domain/application layer, not in controllers or database queries alone.
- **Authoritative Source of Truth**: The API never assumes frontend validations are sufficient. Every request is validated and verified authoritatively.
- **DTOs & Contracts**: Always use Data Transfer Objects (DTOs) for request and response payloads. Never expose domain or database entities directly in API contracts.
- **Simplicity & Testability**: Prefer simple, cohesive services with dependency injection. Avoid unnecessary complexity or premature abstractions.

---

## 3. Reservation Model & Lifecycle

### Time Modeling
- Model reservations strictly with `StartDateTime` and `EndDateTime` (using timezone-aware types such as `DateTimeOffset` or explicit UTC `DateTime` with timezone handling).
- Do not model reservations solely by number of days. Support both full-day reservations and specific check-in / check-out times.

### Lifecycle States
Reservations must transition strictly among valid states:
- `PENDING`: Initial state upon public customer request.
- `CONFIRMED`: Approved by administrator; triggers calendar sync and customer confirmation.
- `REJECTED`: Declined by administrator.
- `CANCELLED`: Cancelled prior to or during stay.
- `EXPIRED`: Reservation request timed out before confirmation.

*Do not add new reservation states without explicit business requirements.*

### Auditability
- Do not physically delete reservations or administrative records.
- Preserve full history through state transitions (`CANCELLED`, `REJECTED`, `EXPIRED`) and audit timestamps.

---

## 4. Availability & Conflict Rules

A time interval is unavailable if it overlaps with:
1. An existing `CONFIRMED` reservation.
2. A `PENDING` reservation (subject to the configured pending expiration/hold policy).
3. An active `BlockedPeriod` (maintenance, personal use, external bookings).
4. Synchronized external Google Calendar busy intervals.

All overlap and availability computations must be implemented in the backend domain/service layer.

---

## 5. Security & Authentication

- **Admin Endpoints**: Require robust authentication (e.g., JWT / ASP.NET Core Identity) and role-based authorization.
- **Public Endpoints**: Open for checking availability and submitting reservation requests. Must implement:
  - Strict input validation and sanitization.
  - Rate limiting and anti-abuse safeguards.
- **Secrets Management**: Never commit secrets, connection strings, or API keys. Use environment variables and ASP.NET Core configuration (`IConfiguration`).
- **Passwords & Tokens**: Never store plaintext passwords or use predictable tokens for private URLs.

---

## 6. External Integrations

### Google Calendar
- Abstract the Google Calendar API behind an interface (e.g., `ICalendarService`).
- Treat Google Calendar as a secondary integration for synchronization and external conflict reading; never as the primary database.
- Synchronize only `CONFIRMED` reservations.
- Keep domain logic decoupled from Google Calendar SDK specifics.

### Notifications & Communication
- Decouple notification dispatch (email, in-app notifications) from the core database transaction (e.g., domain events or background workers).
- WhatsApp links/messages should be generated on demand; do not tightly couple the API to direct WhatsApp APIs unless explicitly required.

---

## 7. Date & Timezone Handling

- Store all timestamps with consistent timezone semantics (preferably UTC / `DateTimeOffset`).
- Interpret incoming date-times consistently according to the configured property timezone.
- Avoid any ambient local server timezone dependencies.

---

## 8. Error Handling & Logging

- **Error Responses**: Return standardized, structured error responses (e.g., RFC 7807 `ProblemDetails`).
- **Security**: Never expose stack traces, SQL queries, or internal exceptions in production responses.
- **Logging**: Use structured logging (`ILogger<T>`) for all key lifecycle events:
  - Reservation request creation, confirmation, rejection, cancellation, and expiration.
  - Authentication and authorization events.
  - Calendar synchronization and notification dispatch outcomes/failures.
- Never log passwords, tokens, API keys, or sensitive personal data.

---

## 9. Testing Guidelines

Backend test coverage must prioritize:
- Reservation lifecycle transitions and validation rules.
- Overlapping date/time availability logic (boundary cases, exact overlaps, partial overlaps).
- Blocked period conflict detection.
- Authentication & authorization checks on protected endpoints.
- Integration/unit tests with mocked external dependencies (`ICalendarService`, notification providers).
