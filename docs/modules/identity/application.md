# Identity - Application

Commands and queries for Identity, organized as vertical slices
([ADR-002](../../decisions/ADR-002-CQRS.md), [ADR-007](../../decisions/ADR-007-In-Process-CQRS-Dispatcher.md)).

Every request returns `Result` or `Result<T>` and passes through the fixed pipeline
(Logging → Validation → Authorization → Transaction → Handler,
[cqrs-pipeline.md](../../diagrams/cqrs-pipeline.md)).

Permission codes below are defined in [permissions-model.md](permissions-model.md). "Anonymous"
means the operation declares no `[RequirePermission]` and is reachable without authentication.

---

## Commands

### Authentication

| Command | Intent | Permission | Domain events |
| --- | --- | --- | --- |
| `AuthenticateUserCommand` | Verify credentials and start a session. | Anonymous | `UserSignedIn`, `SessionStarted`, `RefreshTokenIssued` |
| `RefreshTokenCommand` | Rotate a refresh token and issue new tokens. | Anonymous | `RefreshTokenRotated` (or `RefreshTokenReuseDetected`, `SessionRevoked`) |
| `LogoutCommand` | Revoke the current session. | Authenticated | `UserSignedOut`, `SessionRevoked` |
| `LogoutAllCommand` | Revoke all sessions for the current user. | Authenticated | `SessionRevoked` (per session) |

### Users (administration)

| Command | Intent | Permission | Domain events |
| --- | --- | --- | --- |
| `RegisterUserCommand` | Create a user with credentials and optional roles. | `identity.users.create` | `UserRegistered` |
| `UpdateUserCommand` | Change name and/or email. | `identity.users.update` | `UserNameChanged`, `UserEmailChanged` |
| `ActivateUserCommand` | Activate a pending/disabled user. | `identity.users.manage-status` | `UserActivated` |
| `DeactivateUserCommand` | Disable a user and revoke sessions. | `identity.users.manage-status` | `UserDeactivated`, `SessionRevoked` |
| `LockUserCommand` | Lock a user until a given time. | `identity.users.lock` | `UserLocked` |
| `UnlockUserCommand` | Remove a lockout. | `identity.users.lock` | `UserUnlocked` |
| `AssignRoleToUserCommand` | Assign a role. | `identity.users.assign-role` | `RoleAssignedToUser` |
| `RemoveRoleFromUserCommand` | Remove a role. | `identity.users.assign-role` | `RoleRemovedFromUser` |
| `ResetUserPasswordCommand` | Administrative password reset. | `identity.users.reset-password` | `UserPasswordChanged`, `SessionRevoked` |

### Self-service

| Command | Intent | Permission | Domain events |
| --- | --- | --- | --- |
| `UpdateMyProfileCommand` | Change own display name. | Authenticated | `UserNameChanged` |
| `ChangeMyPasswordCommand` | Change own password (requires current). | Authenticated | `UserPasswordChanged`, `SessionRevoked` (other sessions) |
| `RequestPasswordResetCommand` | Start a reset flow. | Anonymous | `UserPasswordResetRequested` |
| `CompletePasswordResetCommand` | Finish a reset with a token. | Anonymous | `UserPasswordResetCompleted`, `UserPasswordChanged`, `SessionRevoked` |

### Roles

| Command | Intent | Permission | Domain events |
| --- | --- | --- | --- |
| `CreateRoleCommand` | Create a role with permissions. | `identity.roles.create` | `RoleCreated` |
| `RenameRoleCommand` | Rename and/or re-describe a role. | `identity.roles.update` | `RoleRenamed` |
| `SetRolePermissionsCommand` | Replace a role's permissions. | `identity.roles.set-permissions` | `RolePermissionsChanged` |
| `DeleteRoleCommand` | Delete a non-system role. | `identity.roles.delete` | `RoleDeleted` |

---

## Queries

Queries never modify state and never open a transaction. They project directly to DTOs; they never
load aggregates (database-skill).

| Query | Intent | Permission | Returns |
| --- | --- | --- | --- |
| `GetUserByIdQuery` | User detail. | `identity.users.read` | `Result<UserDto>` |
| `ListUsersQuery` | Paged, filtered users. | `identity.users.read` | `Result<PagedResult<UserSummaryDto>>` |
| `GetCurrentUserQuery` | Caller profile and roles. | Authenticated | `Result<CurrentUserDto>` |
| `ListRolesQuery` | All roles. | `identity.roles.read` | `Result<IReadOnlyList<RoleDto>>` |
| `GetRoleByIdQuery` | Role detail with permissions. | `identity.roles.read` | `Result<RoleDto>` |
| `ListPermissionsQuery` | Permission catalog. | `identity.permissions.read` | `Result<IReadOnlyList<PermissionDto>>` |
| `GetMyPermissionsQuery` | Effective permissions of the caller. | Authenticated | `Result<IReadOnlyList<string>>` |
| `ListUserSessionsQuery` | Sessions for a user. | `identity.sessions.read` | `Result<IReadOnlyList<SessionDto>>` |
| `ListMySessionsQuery` | Own sessions. | Authenticated | `Result<IReadOnlyList<SessionDto>>` |

---

## Notes

### Authentication reads credentials inside the command

`AuthenticateUserCommand` and `RefreshTokenCommand` are commands (they create/rotate state).
They load the aggregate through the aggregate repository; they do not use query handlers to read
credentials.

### Password reset is a two-step flow

`RequestPasswordResetCommand` produces a single-use, time-limited reset token and publishes an
integration event; Notifications delivers the email. `CompletePasswordResetCommand` consumes the
token. Identity never sends email itself (see [module-boundaries.md](module-boundaries.md)).

### Provisioning (startup, not user commands)

- `SynchronizePermissions` seeds/updates the permission catalog from module-declared permissions.
- `SeedSystemRoles` provisions system roles (for example `Administrator`) idempotently.

These run at startup through the module's infrastructure, not as user-facing commands.

### Standard behavior

- Validation is mandatory and produces structured field-level errors (ADR-011).
- Protected operations declare `[RequirePermission]` (ADR-012).
- Commands run in a transaction via the unit of work; queries do not.
- Domain events are persisted and dispatched per ADR-009; cross-module integration events are
  written to the Outbox in the same transaction.
