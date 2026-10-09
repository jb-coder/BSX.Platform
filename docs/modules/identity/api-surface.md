# Identity - API Surface

HTTP surface of the Identity module. Every endpoint is a thin transport adapter over a command or
query; it maps `Result` to HTTP via `IErrorMapper` (ADR-011) and contains no business logic.

- Base path: `/identity`
- Authentication: `Authorization: Bearer <access-token>` (except anonymous endpoints)
- Authorization: `[RequirePermission]` per endpoint (ADR-012)
- Content type: `application/json`
- Errors: RFC 9457 `ProblemDetails`, `Validation` populates the `errors` dictionary

---

## Authentication

| Method | Path | Auth | Request | Success |
| --- | --- | --- | --- | --- |
| POST | `/identity/auth/login` | Anonymous | `{ email, password }` | `200` `{ accessToken, tokenType, expiresIn, refreshToken }` |
| POST | `/identity/auth/refresh` | Anonymous | `{ refreshToken }` | `200` same token payload |
| POST | `/identity/auth/logout` | Authenticated | `{ refreshToken }` | `204` |
| POST | `/identity/auth/logout-all` | Authenticated | — | `204` |
| POST | `/identity/auth/password/forgot` | Anonymous | `{ email }` | `202` (always, anti-enumeration) |
| POST | `/identity/auth/password/reset` | Anonymous | `{ token, newPassword }` | `204` |

Login and refresh are anonymous because the credential is in the body. Failures return
`Authentication` (401); invalid reset tokens return `Failure` (422).

---

## Users (administration)

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| POST | `/identity/users` | `identity.users.create` | `201` `{ userId }` |
| GET | `/identity/users?page&size&search` | `identity.users.read` | `200` paged |
| GET | `/identity/users/{userId}` | `identity.users.read` | `200` user |
| PATCH | `/identity/users/{userId}` | `identity.users.update` | `200` |
| POST | `/identity/users/{userId}/activate` | `identity.users.manage-status` | `200` |
| POST | `/identity/users/{userId}/deactivate` | `identity.users.manage-status` | `200` |
| POST | `/identity/users/{userId}/lock` | `identity.users.lock` | `200` |
| POST | `/identity/users/{userId}/unlock` | `identity.users.lock` | `200` |
| POST | `/identity/users/{userId}/roles` | `identity.users.assign-role` | `200` |
| DELETE | `/identity/users/{userId}/roles/{roleId}` | `identity.users.assign-role` | `200` |
| POST | `/identity/users/{userId}/password` | `identity.users.reset-password` | `200` |
| GET | `/identity/users/{userId}/sessions` | `identity.sessions.read` | `200` |
| DELETE | `/identity/users/{userId}/sessions/{sessionId}` | `identity.sessions.revoke` | `204` |

`PATCH` body: `{ name?, email? }`. `lock` body: `{ untilUtc? }`.

---

## Roles

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/identity/roles` | `identity.roles.read` | `200` |
| POST | `/identity/roles` | `identity.roles.create` | `201` `{ roleId }` |
| GET | `/identity/roles/{roleId}` | `identity.roles.read` | `200` |
| PATCH | `/identity/roles/{roleId}` | `identity.roles.update` | `200` |
| PUT | `/identity/roles/{roleId}/permissions` | `identity.roles.set-permissions` | `200` |
| DELETE | `/identity/roles/{roleId}` | `identity.roles.delete` | `204` |

`POST`/`PUT` body includes `permissions: string[]` validated against the catalog.

---

## Permissions

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/identity/permissions` | `identity.permissions.read` | `200` catalog |
| GET | `/identity/me/permissions` | Authenticated | `200` effective permissions |

---

## Current User (self-service)

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/identity/me` | Authenticated | `200` profile + roles |
| PATCH | `/identity/me` | Authenticated | `200` |
| POST | `/identity/me/password` | Authenticated | `204` |
| GET | `/identity/me/sessions` | Authenticated | `200` |
| DELETE | `/identity/me/sessions/{sessionId}` | Authenticated | `204` |

`PATCH` body: `{ name }`. `password` body: `{ currentPassword, newPassword }`.

---

## Discovery

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/identity/.well-known/jwks.json` | Anonymous | `200` public keys |

---

## Standard Responses

| Status | ErrorKind | Meaning |
| --- | --- | --- |
| `400` | `Validation` | Input failed validation; `errors` dictionary per field. |
| `401` | `Authentication` | Missing/invalid credentials or token. |
| `403` | `Permission` | Authenticated but not permitted. |
| `404` | `NotFound` | Resource does not exist. |
| `409` | `Conflict` | Uniqueness or state conflict (for example duplicate email). |
| `422` | `Failure` | Business rule rejected the request. |
| `500` | `Unexpected` | Unhandled fault. |

---

## API Conventions

- Endpoints are grouped by capability and implemented with Minimal API in `Identity.Endpoints`.
- No endpoint contains business rules; each delegates to a command or query through `ISender`.
- Requests are records; responses are DTOs; neither is a domain type.
- `202 Accepted` is used where processing is asynchronous (password reset email).
- Password reset never reveals whether an email exists.
- Login and refresh endpoints are rate-limited.
- Access tokens are never logged; refresh tokens are never logged or echoed.
- The API is versioned; breaking changes follow GitFlow and Semantic Versioning
  ([ADR-005](../../decisions/ADR-005-GitFlow.md)).
