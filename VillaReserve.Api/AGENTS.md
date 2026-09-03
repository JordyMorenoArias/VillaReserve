# VillaReserve.Api — Backend Agent Instructions

## 1. Purpose & Scope

`VillaReserve.Api` is the ASP.NET Core backend for the VillaReserve reservation management system.

This document defines the permanent architectural, structural, technical, security, persistence, and testing rules that AI agents and developers must follow when modifying this backend. Feature specifications define **what must be built**; this file defines **how the backend must be structured and developed**. When a specification conflicts with these rules, the deviation must be explicitly justified.

The backend is the authoritative source of truth for:

- Villa reservations and their lifecycle (`PENDING`, `CONFIRMED`, `REJECTED`, `CANCELLED`, `EXPIRED`).
- Availability calculation and conflict prevention.
- Manually blocked periods (maintenance, personal use, external bookings).
- External calendar synchronization (Google Calendar).
- Administrative authentication and granular authorization.
- Customer and administrator notifications.

Google Calendar is an external integration and conflict source — **never** the primary database. The VillaReserve database is the system of record.

---

## 2. Technology Stack & Baseline Standards

- **Target Framework**: .NET 9 (`net9.0`), C# 13
- **Web Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Database**: PostgreSQL with `pgcrypto` and `btree_gist` extensions
- **Validation**: FluentValidation
- **Documentation**: OpenAPI / Swagger / Scalar (exposed via `/docs`)
- **Health Checks**: exposed via `/health` (app + database connectivity)
- **Testing**: xUnit, FluentAssertions, Moq/NSubstitute, Testcontainers for PostgreSQL

### Compiler & Code Quality

Every `.csproj` must enforce:

```xml
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<ImplicitUsings>enable</ImplicitUsings>
```

- The solution must build with **0 errors and 0 warnings**. Do not use `#pragma warning disable` to hide design problems.
- Avoid dead code, commented-out implementations, unused dependencies, unnecessary abstractions, magic strings/numbers, duplicated configuration, hidden global state.

### Async & Cancellation

- Every asynchronous application, infrastructure, repository, or integration method must accept and propagate a `CancellationToken` when the underlying API supports it.
- Do not create new `CancellationTokenSource` instances in lower layers to replace the request token.
- Never use `.Result` or `.Wait()`. Avoid unnecessary `Task.Run()` for I/O-bound work.

---

## 3. Solution Structure

```text
VillaReserve.Api/
├── src/
│   ├── API/                       # Host, Controllers, Middleware, Filters, Extensions
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Extensions/
│   │
│   ├── Domain/                    # Pure business model — no infra dependencies
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   ├── Errors/
│   │   └── Rules/
│   │
│   ├── Features/                  # Feature-oriented vertical slices (application layer)
│   │   ├── Reservations/
│   │   ├── Availability/
│   │   ├── BlockedPeriods/
│   │   ├── Authentication/
│   │   ├── Notifications/
│   │   └── Calendar/
│   │
│   ├── Infrastructure/            # Persistence, external APIs, DbContext, configs
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   ├── Authentication/
│   │   ├── Calendar/
│   │   ├── Notifications/
│   │   └── Configuration/
│   │
│   └── Shared/                    # Truly cross-cutting primitives only
│       ├── Authorization/
│       ├── Errors/
│       ├── Validation/
│       ├── Results/
│       ├── Constants/
│       └── Extensions/
│
├── tests/
│   ├── Unit/                      # Fast, zero external I/O
│   └── Integration/                # Real PostgreSQL (Testcontainers)
│
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── VillaReserve.Api.sln
```

This structure is mandatory unless a future specification explicitly justifies a change for a specific architectural concern.

---

## 4. Layering Rules & Dependency Inversion

Dependencies always flow inward:

```text
HTTP Request → Controller (API) → Feature Handler (Application)
            → Domain Model & Rules → Infrastructure (via abstractions)
            → PostgreSQL / Google Calendar / External Services
```

1. **Thin Controllers**: only accept DTOs, delegate to feature handlers, return standardized HTTP responses. Never contain business rules, `DbContext` access, complex validation, or direct external SDK calls.
2. **Dependency Inversion**: feature services/handlers depend on interfaces (`IReservationRepository`, `IAvailabilityService`, `ICalendarService`, `IEmailService`, `IUnitOfWork`), never on concrete infrastructure classes. Interfaces are defined in the Application/Feature layer; implementations live in `Infrastructure/`. Avoid vacuous 1:1 interfaces with no real benefit, but always abstract I/O, external integrations, and persistence boundaries.
3. **DTOs & Encapsulation**: EF Core entities are never accepted as controller parameters or returned directly in responses — always explicit Request/Response DTOs. Don't leak `IQueryable<T>` out of repository boundaries if it bypasses invariants or leaks tracking details.
4. **Service Registration**: encapsulate registrations in dedicated extension methods (e.g., `builder.Services.AddInfrastructure(builder.Configuration)`).

---

## 5. Folder Ownership Rules

Each folder has a specific responsibility. Do not place files in another layer merely because it's convenient.

| Type                                                             | Location                                         |
| ---------------------------------------------------------------- | ------------------------------------------------ |
| Domain Entity                                                    | `src/Domain/Entities/`                           |
| Domain Enum                                                      | `src/Domain/Enums/`                              |
| Value Object                                                     | `src/Domain/ValueObjects/`                       |
| Domain Error                                                     | `src/Domain/Errors/`                             |
| Domain Rule                                                      | `src/Domain/Rules/`                              |
| Feature Handler / Request / Response DTO / Validator / Interface | `src/Features/<Feature>/`                        |
| Controller                                                       | `src/API/Controllers/`                           |
| Middleware                                                       | `src/API/Middleware/`                            |
| API Filter                                                       | `src/API/Filters/`                               |
| API Extension                                                    | `src/API/Extensions/`                            |
| DbContext                                                        | `src/Infrastructure/Persistence/`                |
| EF Configuration                                                 | `src/Infrastructure/Persistence/Configurations/` |
| EF Migration                                                     | `src/Infrastructure/Persistence/Migrations/`     |
| Repository Implementation                                        | `src/Infrastructure/Persistence/Repositories/`   |
| External Integration (Calendar, Email, etc.)                     | `src/Infrastructure/<Integration>/`              |
| Shared Error/Validation/Result/Constant/Extension Infrastructure | `src/Shared/<Concern>/`                          |
| Unit Test                                                        | `tests/Unit/`                                    |
| Integration Test                                                 | `tests/Integration/`                             |

### Domain Layer specifics

- The Domain layer (`src/Domain/`) must not depend on ASP.NET Core, EF Core, PostgreSQL, Google APIs, HTTP, or any infrastructure technology.
- Domain entities must not contain EF Core mapping attributes — keep persistence concerns in `Infrastructure/Persistence/Configurations/`.
- Domain errors must not depend on HTTP status codes (no `BadRequestException`, `NotFoundHttpException`, etc. inside `Domain/`).
- Value objects should represent a meaningful domain concept with real invariants — not be created for abstraction's sake.

### Prohibited structural patterns

Do not introduce global catch-all folders used as dumping grounds without architectural justification:

```text
src/Services/   src/Repositories/   src/DTOs/   src/Validators/
src/Helpers/    src/Managers/       src/Utils/  src/Models/
```

Examples of misplacement to avoid:

- `Reservation.cs` inside `Infrastructure/Persistence/` (belongs in `Domain/Entities/`).
- `ReservationConfiguration.cs` inside `Domain/Entities/` (belongs in `Infrastructure/Persistence/Configurations/`).
- `CreateReservationValidator.cs` inside `Shared/Validation/` if it's specific to Reservations (belongs in `Features/Reservations/`).
- `GoogleCalendarService.cs` (SDK implementation) inside `Features/Calendar/` (belongs in `Infrastructure/Calendar/`).

---

## 6. Security & Authorization

Uses ASP.NET Core's native role-based and policy-based authorization.

**Role-based** (coarse-grained admin access):

```csharp
[Authorize(Roles = AppRoles.Admin)]
```

Role names must be strongly-typed constants (`AppRoles.Admin`), never magic strings.

**Policy-based** (complex business permissions), registered at startup:

```csharp
options.AddPolicy(AppPolicies.CanManageReservations, policy =>
    policy.RequireRole(AppRoles.Admin, AppRoles.Manager));
```

**Custom requirements/handlers** for dynamic or resource-based conditions:

```csharp
public class MustBePendingReservationRequirement : IAuthorizationRequirement { }

public class MustBePendingReservationHandler
    : AuthorizationHandler<MustBePendingReservationRequirement, Reservation>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBePendingReservationRequirement requirement,
        Reservation resource)
    {
        if (resource.Status == ReservationStatus.Pending)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

**Public endpoints** (availability checks, reservation requests) must:

- Validate/sanitize all inputs via FluentValidation before domain logic executes.
- Implement rate-limiting to prevent scraping and DoS.
- Never expose internal database identifiers, stack traces, or administrative metadata.

---

## 7. Persistence & EF Core

- Single `AppDbContext` in `Infrastructure/Persistence/AppDbContext.cs`.
- Dedicated `IEntityTypeConfiguration<T>` classes in `Infrastructure/Persistence/Configurations/`.
- `Guid` (PostgreSQL UUID) for all primary keys and external references.
- Extensions activated via migrations: `modelBuilder.HasPostgresExtension("pgcrypto")` and `HasPostgresExtension("btree_gist")`.
- Schema managed exclusively through EF Core migrations (`dotnet ef migrations add`, `dotnet ef database update`) — never manual production SQL.
- Optimistic concurrency (`RowVersion`/concurrency tokens) on reservations and blocked periods to prevent double-booking races.
- **No generic repository by default**: prefer feature-specific abstractions (`IReservationRepository`) over `IRepository<T>` for the whole app, unless there's a demonstrated need.
- Queries stay close to the feature that owns them — no giant global query/service layer. Project only required columns when appropriate, avoid unnecessary tracking on reads, avoid N+1, propagate `CancellationToken`.
- **Transactions**: use only when multiple persistence operations must succeed/fail together. Don't wrap every operation unnecessarily. External API calls (Google Calendar, email) should generally not occur inside a DB transaction — prefer persistence-first design with reliable async processing (outbox pattern, background jobs, domain events).

---

## 8. Date & Timezone Standards

- Persist business timestamps strictly as `TIMESTAMPTZ` (`timestamp with time zone`).
- Use `DateTimeOffset` or UTC `DateTime` for all business dates/times (`StartDateTime`, `EndDateTime`).
- Never rely on the host server's local timezone. Store and compute in UTC; convert to the configured property timezone only when evaluating local business rules (e.g., check-in/check-out hours).
- **Interval overlap** between two half-open intervals `[Start_A, End_A)` and `[Start_B, End_B)`:
  $$\text{Overlap} \iff (Start_A < End_B) \land (End_A > Start_B)$$

---

## 9. Error Handling & Validation

- Centralized exception handling middleware / `IExceptionHandler` mapping exceptions to RFC 7807 `ProblemDetails`.
- Unhandled errors return `500` with a generic message and correlation ID. Never leak connection strings, stack traces, schema details, or third-party API keys.
- All incoming commands/requests validated via FluentValidation before reaching domain logic. Validation failures return `400` with `ValidationProblemDetails`.
- Prefer explicit domain error objects / structured Result types for anticipated business failures (e.g., `SlotUnavailableError`, `InvalidStateTransitionError`) instead of throwing exceptions for expected outcomes.

---

## 10. Configuration & Secrets

- Strongly-typed configuration classes (`IOptions<T>`) with `ValidateDataAnnotations()` and `ValidateOnStart()` to fail fast on missing settings.
- Never commit credentials, passwords, API keys, or connection strings. Maintain `.env.example` with dummy values.
- Local development uses environment variables or Docker Compose environment files.

---

## 11. External Integrations

- Google Calendar and notification dispatching (Email, WhatsApp link generators) abstracted behind interfaces (`ICalendarService`, `IEmailNotificationService`).
- Core database transactions must not fail or hang if an external service is slow/unavailable — dispatch via background jobs, outbox pattern, or domain events.
- Only `CONFIRMED` reservations sync to Google Calendar.

---

## 12. Testing Standards

```text
tests/
├── Unit/               # Domain logic, validators, handlers, overlap math — zero external I/O
└── Integration/         # Real PostgreSQL (Testcontainers) — migrations, repos, /health, full API pipelines
```

- **Strict prohibition**: SQLite must never substitute PostgreSQL in tests — `TIMESTAMPTZ`, UUIDs, `btree_gist`, and exclusion constraints are PostgreSQL-specific.
- All tests run via `dotnet test`.

---

## 13. Avoiding Premature Abstraction

Do not introduce generic repositories, generic service layers, generic managers, generic factories, or generic wrappers unless the project has a demonstrated need. Prefer concrete, simple implementations until an abstraction provides real architectural benefit.

---

## 14. AI Agent Workflow

When implementing a specification:

1. Read this `AGENTS.md`.
2. Read the relevant specification.
3. Inspect the existing implementation.
4. Preserve existing architectural conventions; reuse existing abstractions.
5. Implement only the requested scope — do not invent unrelated functionality or opportunistically refactor unrelated code.
6. Add or update tests for meaningful behavior.
7. Run build and tests; verify migrations when applicable.
8. Report deviations explicitly.

### Handling ambiguous requirements

Do not silently invent business rules. If a requirement is ambiguous:

1. Check the existing domain model, this file, relevant specs, and existing implementation.
2. Identify whether a safe technical default exists.
3. If the ambiguity materially affects business behavior, surface it rather than guessing.

Technical implementation details may be chosen freely when they don't change externally observable business behavior. Business rules must not be invented without justification.

### Definition of Done

A backend change is complete only when:

- The implementation follows this architecture and canonical file locations.
- The relevant specification is satisfied; domain boundaries are preserved.
- Persistence mappings are correctly configured; validation is implemented where required.
- Errors are handled consistently.
- Automated tests cover meaningful behavior; PostgreSQL-specific behavior is tested against real PostgreSQL.
- `dotnet build` succeeds with 0 warnings; `dotnet test` succeeds.
- Required migrations are created and validated.
- No secrets introduced; no unrelated functionality added.

---

## 15. Final Architectural Principle

The backend should remain: **Simple · Modular · Explicit · Testable · Secure · Database-aware · Feature-oriented · Domain-oriented · Infrastructure-independent**.

Prefer clear code over clever code. Prefer explicit boundaries over implicit conventions. Prefer a small number of meaningful abstractions over a large abstraction framework. The architecture exists to make VillaReserve easier to understand, test, modify, and maintain — not to add complexity for its own sake.
