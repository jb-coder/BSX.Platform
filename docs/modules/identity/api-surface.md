# Identity - API Surface

HTTP surface of the Identity module. Every mapped endpoint is a thin transport adapter over a
command or query; it maps `Result` to HTTP via `IErrorMapper` (ADR-011) and contains no business
logic.

- Base path: `/identity`
- Authentication: `Authorization: Bearer <access-token>` except anonymous endpoints
- Authorization: command/query permission declarations, enforced by the CQRS authorization behavior
- Content type: `application/json`
- Errors: RFC 9457 `ProblemDetails`; validation populates the `errors` dictionary

This document reflects the endpoints mapped by `IdentityEndpointRouteBuilderExtensions`. The web
administration UI uses in-process application dispatch and does not depend on the endpoints whose
HTTP adapters are not listed here.

---

## Authentication

| Method | Path | Auth | Request | Success |
| --- | --- | --- | --- | --- |
| POST | `/identity/login` | Anonymous | `{ email, password, tenantId? }` | `200` `{ accessToken, accessTokenExpiresAtUtc, refreshToken }` |
| POST | `/identity/login/tenants` | Anonymous | `{ email, password }` | `200` active tenant options for the web workspace chooser |
| POST | `/identity/refresh` | Anonymous | `{ refreshToken }` | `200` same token payload |
| POST | `/identity/password-reset/request` | Anonymous | `{ email }` | `204`, or `503` when delivery is unavailable |
| POST | `/identity/password-reset/complete` | Anonymous | `{ token, newPassword }` | `204` |
| POST | `/identity/logout` | Anonymous | `{ refreshToken }` | `204` |
| POST | `/identity/logout-all` | Authenticated | - | `204` |

Login and refresh are anonymous because the credential is in the body. Invalid refresh tokens and
revoked tenant memberships return `401`; invalid reset tokens return `422`; a disabled delivery
channel returns `503`. When configured, the web host's SMTP notifier sends the reset link; see
[password recovery](../../identity/password-recovery.md).

---

## Users (administration)

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| POST | `/identity/users` | `identity.users.create` | `201` `{ userId }` |
| GET | `/identity/users?page&size&search` | `identity.users.read` | `200` paged |
| GET | `/identity/users/{userId}` | `identity.users.read` | `200` user |
| POST | `/identity/users/{userId}/unlock` | `identity.users.manage-status` | `204` |

`POST` body: `{ email, name, password? }`. User profile changes, activation/deactivation and
platform-role assignment are available through the web administration application layer, not as
mapped Identity HTTP endpoints in this release.

---

## Roles and memberships

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/identity/roles` | `identity.roles.read` | `200` |
| POST | `/identity/roles` | `identity.roles.create` | `201` `{ roleId }` |
| POST | `/identity/memberships` | `identity.memberships.manage` | `201` `{ membershipId }` |

`POST /identity/roles` body: `{ name, description?, permissions: string[] }`. The endpoint resolves
the tenant from the authenticated token, never from request input.

Role detail/edit/permissions, membership acceptance/role change/removal and membership listing are
implemented in the web administration experience through in-process application dispatch. Role
detail is tenant-bound: a tenant role outside the caller's active tenant is rejected, while
platform/system role definitions remain read-only shared metadata.

---

## Sessions

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/identity/sessions` | Authenticated | `200` current user's sessions |
| DELETE | `/identity/sessions/{id:guid}` | Authenticated | `204` |

Only the current user's sessions are listed or revocable through these endpoints.

---

## Not mapped in this release

The following are application/web capabilities or future integrations, not Identity HTTP endpoints:

- `GET /identity/roles/{roleId}`, role update/delete and role permission replacement.
- User activation/deactivation, profile update, lock and platform-role assignment.
- Membership acceptance, role change and removal.
- Permission catalog listing.
- Current-user profile/password APIs (`/identity/me/*`).
- JWKS discovery. Validation keys are distributed through the deployment secret store.

Adding any of these later must preserve the same tenant guard, permission declaration and
authorization revalidation rules as the application commands and queries.

---

## Standard Responses

| Status | ErrorKind | Meaning |
| --- | --- | --- |
| `400` | `Validation` | Input failed validation; `errors` dictionary per field. |
| `401` | `Authentication` | Missing/invalid credentials, token or refresh. |
| `403` | `Permission` | Authenticated but not permitted, including cross-tenant access. |
| `404` | `NotFound` | Resource does not exist. |
| `409` | `Conflict` | Uniqueness, state or concurrency conflict. |
| `422` | `Failure` | Business rule rejected the request. |
| `503` | `Unavailable` | Password-reset delivery is not configured. |
| `500` | `Unexpected` | Unhandled fault. |

---

## API Conventions

- Endpoints are grouped by capability and implemented with Minimal API in `Identity.Endpoints`.
- No endpoint contains business rules; each delegates to a command or query through `ISender`.
- Requests are records; responses are DTOs; neither is a domain type.
- Password reset never reveals whether an email exists.
- Login, tenant discovery and refresh are rate-limited.
- Tenant-scoped reads and writes resolve the tenant from the validated token and enforce the
  tenant guard in the application layer.
