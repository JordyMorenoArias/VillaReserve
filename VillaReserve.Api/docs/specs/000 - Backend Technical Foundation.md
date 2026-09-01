# Spec: 000 - Backend Technical Foundation

## Objective

Establish the initial technical foundation for the VillaReserve backend using ASP.NET Core, C#, Entity Framework Core, and PostgreSQL.

The goal is to create a stable, modular, testable, and production-oriented backend foundation that is ready for the implementation of business features in subsequent specifications.

This specification must **not implement business functionality** such as authentication, reservations, availability, notifications, or Google Calendar integration.

---

## Context

VillaReserve is a web application for managing reservations for a villa.

The backend will eventually be responsible for:

- Reservation management
- Availability calculation
- Manual blocked periods
- Administrator authentication and authorization
- Notifications
- Google Calendar integration
- Reservation access tokens
- Audit logging

This specification only establishes the technical infrastructure required to build those features safely and consistently.

---

# Scope

## In Scope

This specification includes:

- ASP.NET Core Web API project initialization
- C# configuration
- Entity Framework Core configuration
- PostgreSQL integration
- Docker Compose development environment
- Dependency Injection configuration
- Application configuration management
- Environment variable support
- Global exception handling
- RFC 7807 `ProblemDetails`
- Request validation infrastructure
- Health checks
- OpenAPI/API documentation
- Unit testing infrastructure
- Integration testing infrastructure
- EF Core migrations
- PostgreSQL extensions required by the future architecture
- Basic project structure
- Basic code-quality configuration

## Out of Scope

The following must **not** be implemented as part of this specification:

- User authentication
- JWT
- Authorization policies
- Reservation management
- Availability management
- Blocked periods
- Notifications
- Email integration
- WhatsApp integration
- Google Calendar integration
- Reservation access tokens
- Administration dashboard
- Business/domain entities
- Business workflows
- Background jobs
- Frontend implementation
- Production CI/CD
- Production infrastructure

---

# Technical Requirements

## RT-01 - Technology Stack

The backend must use:

- .NET SDK `9.0`
- ASP.NET Core Web API
- C# `13`
- Entity Framework Core `9`
- PostgreSQL
- Npgsql EF Core provider
- `dotnet-ef`
- xUnit for automated tests

The project must enable nullable reference types.

The project must treat compiler warnings as errors.

The expected configuration is conceptually equivalent to:

```xml
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

No unnecessary framework or third-party dependency should be introduced.

---

## RT-02 - Project Structure

The backend must use a modular structure organized around technical responsibilities and future features.

The initial structure should be:

```text
VillaReserve.Api/
├── src/
│   ├── API/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Extensions/
│   │
│   ├── Features/
│   │   └── [future features]
│   │
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   └── Configuration/
│   │
│   └── Shared/
│       ├── Errors/
│       ├── Validation/
│       └── Extensions/
│
├── tests/
│   ├── Unit/
│   └── Integration/
│
├── Dockerfile
├── docker-compose.yml
└── VillaReserve.Api.sln
```

The implementation may introduce additional folders when technically justified, but must preserve the general modular organization.

---

## RT-03 - Feature-Oriented Organization

Business functionality must eventually be organized by feature rather than by large global technical folders.

Future features are expected to include:

```text
Features/
├── Reservations/
├── Availability/
├── BlockedPeriods/
├── Authentication/
├── Notifications/
└── Calendar/
```

Avoid creating global catch-all folders such as:

```text
Services/
Repositories/
DTOs/
Validators/
Helpers/
```

when those classes belong specifically to a feature.

Shared infrastructure should only contain genuinely shared functionality.

---

## RT-04 - Application Layering

The backend should follow a clear dependency direction:

```text
HTTP Request
     ↓
Controller
     ↓
Feature / Use Case
     ↓
Domain / Business Logic
     ↓
Infrastructure
     ↓
PostgreSQL / External Services
```

Controllers must remain thin.

Controllers must not contain:

- Business rules
- Database queries
- Complex validation
- External service orchestration
- Reservation logic

Business logic must be implemented outside controllers.

Interfaces must not be created merely for the sake of abstraction. Introduce abstractions when they provide a meaningful architectural or testing benefit.

---

## RT-05 - Dependency Injection

Dependencies must be registered through ASP.NET Core Dependency Injection.

Infrastructure registration should be encapsulated in an extension method such as:

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

`Program.cs` should remain primarily responsible for application composition and startup configuration.

---

## RT-06 - Entity Framework Core

Entity Framework Core must be used as the ORM.

PostgreSQL must be accessed through the Npgsql provider.

The application must expose a single application database context:

```text
Infrastructure/
└── Persistence/
    └── AppDbContext.cs
```

EF Core entity configuration should preferably use:

```text
IEntityTypeConfiguration<T>
```

when entity mappings become sufficiently complex.

EF Core entities must not be exposed directly through API responses.

API contracts must use DTOs.

---

## RT-07 - PostgreSQL Development Environment

The project must provide a Docker Compose configuration for local PostgreSQL development.

The PostgreSQL container must:

- Use an official PostgreSQL image
- Persist database data through a Docker volume
- Receive credentials through environment variables
- Avoid hardcoded secrets
- Provide a health check
- Be usable by the backend during local development

The database connection must be configurable through environment variables.

---

## RT-08 - Configuration Management

Application configuration must support:

- `appsettings.json`
- `appsettings.Development.json`
- Environment variables

Typed configuration should use the Options pattern:

```csharp
IOptions<T>
```

Required configuration must be validated during application startup.

If a required configuration value is missing or invalid, the application must fail fast with a descriptive error.

---

## RT-09 - Secrets

Secrets must never be committed to source control.

The repository must include an example configuration file such as:

```text
.env.example
```

The example file may contain placeholder values but must not contain real credentials, passwords, tokens, API keys, or connection strings containing secrets.

---

## RT-10 - Global Exception Handling

The API must provide centralized exception handling.

Unhandled exceptions must not expose:

- Stack traces
- Internal implementation details
- Database credentials
- Connection strings
- Sensitive infrastructure information

The API must return standardized error responses using RFC 7807 `ProblemDetails`.

---

## RT-11 - Request Validation

The backend must have a consistent request-validation mechanism.

FluentValidation should be used for complex or feature-specific validation.

Validation errors must be returned using an appropriate `ValidationProblemDetails` response.

Validation must occur before executing business operations.

---

## RT-12 - Health Check

The backend must expose:

```http
GET /health
```

The health endpoint must verify that:

1. The application is running.
2. PostgreSQL is reachable.

When all required dependencies are healthy, the endpoint must return HTTP `200`.

When PostgreSQL is unavailable, the health check must report an unhealthy state.

---

## RT-13 - API Documentation

The backend must expose automatically generated OpenAPI documentation.

The project may use Swagger UI, Scalar, or an equivalent OpenAPI interface.

The documentation must be accessible through a predictable route such as:

```text
/docs
```

The exact documentation UI may vary as long as the OpenAPI contract is available.

The `/health` endpoint must appear in the API documentation.

---

## RT-14 - Initial API Surface

The technical foundation must not introduce fake business endpoints.

The only required application endpoint at this stage is:

```http
GET /health
```

Business endpoints must be introduced by subsequent feature specifications.

A request to an undefined endpoint must return:

```http
404 Not Found
```

using the configured API error format where applicable.

---

## RT-15 - Database Migrations

EF Core migrations must be used to manage database schema changes.

The initial migration infrastructure must be functional.

The database must be capable of being initialized through:

```bash
dotnet ef database update
```

Database schema changes must be represented through migrations rather than manual production-only SQL.

---

## RT-16 - PostgreSQL Extensions

The database foundation must support the PostgreSQL extensions required by the future VillaReserve reservation architecture:

```sql
pgcrypto
btree_gist
```

These extensions must be enabled through the migration system rather than relying on manual database configuration.

`pgcrypto` will support PostgreSQL-generated UUIDs where appropriate.

`btree_gist` will be required later for PostgreSQL exclusion constraints used to prevent overlapping reservations.

This specification does not implement the reservation constraint itself.

---

## RT-17 - Identifier Strategy

The backend must use UUID/GUID identifiers for persistent entities.

The implementation should use:

```text
Guid
```

in the C# domain/application model and PostgreSQL-compatible UUID columns.

---

## RT-18 - Date and Time Strategy

Date/time values representing VillaReserve business events must eventually use timezone-aware timestamps.

PostgreSQL should use:

```text
TIMESTAMPTZ
```

for persisted business date/time values.

The official VillaReserve business timezone must be explicitly configured before implementing reservation functionality.

This specification does not define the final business timezone.

---

## RT-19 - Automated Testing Structure

The project must provide separate automated testing areas:

```text
tests/
├── Unit/
└── Integration/
```

At minimum, the foundation must contain:

- One meaningful unit test
- One meaningful integration test

Tests must be executable through:

```bash
dotnet test
```

---

## RT-20 - Integration Testing Database

Integration tests must use PostgreSQL or a PostgreSQL-compatible environment.

SQLite must not be used as a replacement for PostgreSQL integration tests.

This is important because future VillaReserve functionality will depend on PostgreSQL-specific features such as:

- `TIMESTAMPTZ`
- UUID support
- GiST indexes
- Exclusion constraints
- PostgreSQL extensions

---

## RT-21 - Code Quality

The project must maintain a clean baseline.

The implementation must:

- Compile without warnings
- Keep nullable reference types enabled
- Avoid dead code
- Avoid commented-out abandoned implementations
- Avoid unnecessary abstractions
- Avoid duplicated configuration
- Avoid hardcoded secrets
- Follow consistent naming conventions
- Keep dependencies minimal and justified

---

# Technical Acceptance Criteria

## CA-01 - Build

Running:

```bash
dotnet restore
dotnet build
```

must complete successfully with:

- 0 compilation errors
- 0 compiler warnings

---

## CA-02 - PostgreSQL

Running:

```bash
docker compose up -d
```

must start PostgreSQL successfully.

The PostgreSQL container must report a healthy state.

Database data must persist after restarting the container.

---

## CA-03 - Database Migration

Running:

```bash
dotnet ef database update
```

must successfully apply the initial migration.

The required PostgreSQL extensions must be enabled:

```text
pgcrypto
btree_gist
```

---

## CA-04 - Health Endpoint

With the application and database running:

```http
GET /health
```

must return:

```http
200 OK
```

When PostgreSQL is unavailable, the health check must report an unhealthy state.

---

## CA-05 - API Documentation

The API documentation must be accessible through the configured documentation route.

The OpenAPI document must contain the `/health` endpoint.

---

## CA-06 - Unknown Endpoint

Requesting an undefined endpoint must return:

```http
404 Not Found
```

The response must follow the API's standardized error format where supported.

---

## CA-07 - Missing Required Configuration

If a required configuration value is removed or invalidated, the application must fail during startup.

The error must identify the missing or invalid configuration without exposing secrets.

---

## CA-08 - Automated Tests

Running:

```bash
dotnet test
```

must complete successfully.

The test suite must contain at least:

- One unit test
- One integration test

---

## CA-09 - Fresh Environment

A developer cloning the repository must be able to perform the following sequence:

```bash
docker compose up -d
dotnet ef database update
dotnet run
```

and obtain a functioning backend foundation.

The developer must be able to access:

```text
/health
/docs
```

without manually modifying source code.

---

# Expected Result

At the end of this specification, VillaReserve must have a functioning backend foundation with:

- ASP.NET Core configured
- C# configured
- EF Core configured
- PostgreSQL configured
- Docker Compose development environment
- Dependency Injection
- Typed configuration
- Environment variable support
- Global error handling
- `ProblemDetails`
- Request validation infrastructure
- Health checks
- OpenAPI documentation
- EF Core migrations
- PostgreSQL extensions
- Unit testing infrastructure
- Integration testing infrastructure
- Basic modular architecture

No reservation or other business functionality should be implemented yet.

The result should be a clean technical baseline from which the subsequent VillaReserve feature specifications can be implemented.

---

# Future Specifications

The expected progression after this foundation is:

```text
000 - Backend Technical Foundation
001 - Authentication
002 - Reservations
003 - Availability
004 - Blocked Periods
005 - Notifications
006 - Google Calendar
007 - Reservation Access Tokens
008 - Administration
```

Each subsequent specification must define the functional and technical requirements for its own feature without duplicating the foundation defined here.
