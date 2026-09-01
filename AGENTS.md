# VillaReserve — Agent Instructions

## 1. Project Overview

VillaReserve is a web-based villa reservation management system.

The system is intended to reduce the manual work involved in managing villa reservations through Google Calendar and direct communication with customers.

The system has two main applications:

- `VillaReserve.Api`: ASP.NET Core backend.
- `VillaReserve.Web`: Angular frontend.

The backend is the source of truth for reservation and availability data.

Google Calendar is an external integration and must not be treated as the primary database.

---

## 2. Core Business Flow

The intended reservation flow is:

1. A customer visits the public website.
2. The customer checks villa availability.
3. The customer selects a start date/time and an end date/time.
4. The customer provides the required contact information.
5. The system creates a reservation request with `PENDING` status.
6. The administrator receives a notification.
7. The administrator reviews the request.
8. The administrator can confirm or reject the request.
9. A confirmed reservation is synchronized with Google Calendar.
10. The customer is notified of the result.

Customers do not create accounts or log into the system.

The administrator is the authenticated user responsible for managing reservations.

---

## 3. Important Business Concepts

### Reservation

A reservation represents a period during which the villa is requested or occupied.

Reservations are modeled using a start and end date/time.

Do not model the reservation primarily as a number of days.

Use:

- `startDateTime`
- `endDateTime`

This allows the system to support both:

- Full-day reservations.
- Reservations with specific check-in and check-out times.

### Reservation States

The currently defined states are:

- `PENDING`
- `CONFIRMED`
- `REJECTED`
- `CANCELLED`
- `EXPIRED`

Do not introduce additional states without a clear business requirement.

### Availability

A time interval is unavailable when it conflicts with an applicable:

- Confirmed reservation.
- Pending reservation, according to the pending-reservation policy.
- Manually blocked period.
- Google Calendar event.

Availability rules must be centralized and must not be duplicated independently in the frontend and backend.

The backend is authoritative when determining availability.

### Blocked Periods

Administrators can manually block a date/time interval.

A blocked period can represent:

- Maintenance.
- Personal use.
- External reservation.
- Other operational reasons.

The exact business rules for blocked periods must be defined before implementation if they affect reservation behavior.

---

## 4. Customer Accounts

Customers must not be required to register or authenticate.

Customer information belongs to a reservation/request and is not currently modeled as a customer account.

Avoid introducing customer authentication unless a future requirement explicitly justifies it.

---

## 5. WhatsApp

WhatsApp is a communication channel, not the reservation database.

The initial implementation should prefer a simple WhatsApp flow where the system can open WhatsApp with a pre-filled message.

Do not introduce WhatsApp Business API integration unless explicitly requested or required.

The reservation request must be stored by the system independently of whether the customer sends the WhatsApp message.

---

## 6. Notifications

The system should support administrator notifications for new reservation requests.

The initial notification strategy may include:

- In-app notifications.
- Email notifications.

Customer notifications may include email.

Do not assume automated WhatsApp notifications unless explicitly specified.

Notification delivery should be decoupled from the core reservation transaction whenever practical.

---

## 7. Google Calendar

Google Calendar is an external integration.

The system database remains the source of truth for application-managed reservations.

Confirmed reservations should be synchronized with Google Calendar.

The current business assumption is that existing Google Calendar events represent periods when the villa is unavailable.

This assumption must be isolated behind the calendar integration and should not leak throughout the domain logic.

Do not couple core reservation logic directly to Google Calendar APIs.

---

## 8. Security

Security is a first-class requirement.

Never:

- Store plaintext passwords.
- Trust client-side validation as a security boundary.
- Expose sensitive administrative information through public endpoints.
- Use predictable tokens for private reservation links.
- Put secrets directly in source code.
- Commit credentials or API keys.

Administrative operations must require authentication and authorization.

Public reservation endpoints must validate and sanitize incoming data.

Rate limiting and anti-abuse mechanisms should be considered for public endpoints.

---

## 9. Architecture Principles

Prefer:

- Clear separation of responsibilities.
- Explicit domain rules.
- Dependency injection.
- Small cohesive services.
- Strong typing.
- Centralized business rules.
- Testable components.
- Explicit error handling.
- Secure defaults.

Avoid:

- Premature abstractions.
- Unnecessary design patterns.
- Over-engineering.
- Duplicated business logic.
- Large classes with unrelated responsibilities.
- Magic strings for domain states.
- Business logic inside UI components.

Use the simplest architecture that adequately supports the current requirements.

---

## 10. API and Frontend Contract

The backend API is the contract consumed by the frontend.

When changing an API contract:

1. Identify affected frontend functionality.
2. Update backend and frontend consistently.
3. Update tests.
4. Avoid silently breaking existing consumers.

DTOs should be used to define API contracts.

Do not expose persistence entities directly as API contracts unless there is a clear reason to do so.

---

## 11. Date and Time

Date/time handling is critical to this system.

Reservation intervals must always have an explicit interpretation of their timezone.

Do not use ambiguous date/time representations.

Do not perform date availability calculations using frontend-local assumptions.

The backend must perform authoritative date/time validation.

The system should use a consistent timezone strategy throughout the application.

The exact business timezone and storage strategy must be explicitly defined before production implementation.

---

## 12. Testing

Tests should prioritize business-critical behavior.

At minimum, cover:

- Reservation creation.
- Availability validation.
- Overlapping reservations.
- Blocked periods.
- Reservation state transitions.
- Cancellation.
- Expiration.
- Authorization.
- Calendar synchronization behavior.

Prefer testing business rules independently from external integrations.

External integrations should be abstracted so they can be mocked or replaced in tests.

---

## 13. Error Handling

Errors should be:

- Explicit.
- Consistent.
- Safe to expose.
- Useful to the caller.

Do not expose stack traces, secrets, database internals, or infrastructure details through production API responses.

Frontend applications should present user-friendly error messages without exposing internal implementation details.

---

## 14. Logging

Log important application events, especially:

- Reservation creation.
- Reservation confirmation.
- Reservation rejection.
- Reservation cancellation.
- Reservation expiration.
- Calendar synchronization failures.
- Notification failures.
- Authentication/security events.

Do not log passwords, tokens, API keys, or unnecessary personal information.

---

## 15. Auditability

Reservation deletion should generally be avoided.

Important business records should preserve their history through state changes such as:

- `CANCELLED`
- `REJECTED`
- `EXPIRED`

rather than being physically deleted.

Administrative actions should be auditable where appropriate.

---

## 16. Development Guidelines

Before implementing a feature:

1. Understand the existing architecture.
2. Check the relevant specification.
3. Identify affected modules.
4. Reuse existing patterns where appropriate.
5. Implement the smallest complete solution.
6. Add or update tests.
7. Verify the API/frontend contract.
8. Review security and error handling.

Do not modify unrelated code simply for stylistic reasons.

Do not introduce new dependencies without justification.

---

## 17. Specifications

Specifications define **what the system must do**.

Agent instructions define **how the codebase should be worked on**.

Do not put detailed feature requirements into this file.

Feature-specific requirements should live in the project's specification/documentation system.

When a specification conflicts with an architectural assumption in this file, identify the conflict before implementing the feature.

---

## 18. Unknown Requirements

Do not invent business rules.

If a requirement materially affects:

- Availability.
- Reservation lifecycle.
- Cancellation.
- Notifications.
- Pricing.
- Calendar synchronization.
- Customer communication.
- Security.

and the behavior has not been defined, ask for clarification or explicitly document the assumption before implementing it.

---

## 19. Code Quality

Prioritize:

1. Correctness.
2. Security.
3. Maintainability.
4. Testability.
5. Simplicity.
6. Performance.

Do not optimize prematurely.

A simpler implementation that correctly models the business domain is preferred over a complex abstraction without a demonstrated need.