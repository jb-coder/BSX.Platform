# Identity - Domain Model

Domain layer design for Identity. Business rules live here and nowhere else
([ddd-skill.md](../../../.opencode/skills/ddd-skill.md), architecture Rule 2).

Every aggregate:

- is the single entry point to its consistency boundary;
- raises domain events (intra-module, ADR-009);
- returns `Result` for expected failures (ADR-011);
- exposes no persistence concerns (auditing is shadow-property based, ADR-010).

---

## Aggregates

### User (aggregate root)

The identity of a person who can access the platform.

| Member | Kind | Notes |
| --- | --- | --- |
| `Id` | Value object `UserId` | Strongly typed identity. |
| `Email` | Value object `Email` | Globally unique, validated, normalized (ADR-013). |
| `Name` | Value object `PersonName` | Display name. |
| `Credentials` | Child entities `Credential` | Password, TOTP, recovery codes, passkey, external provider (ADR-014). Replaces the single `PasswordCredential`. |
| `Status` | Value object `UserStatus` | `Pending`, `Active`, `Disabled`, `Locked`. |
| `RoleIds` | Value set `RoleId` | **Superseded (ADR-013):** tenant roles now live on `TenantMembership`; only platform roles may remain on the user. |
| `SecurityStamp` | Value object `SecurityStamp` | Rotated on credential/status/role changes. |
| `AccessFailedCount` | `int` | Failed sign-in attempts. |
| `LockoutEndUtc` | `DateTimeOffset?` | Lockout expiry. |

Behaviors (intention-revealing): `Register`, `ChangeEmail`, `ChangeName`, `AssignRole`,
`RemoveRole`, `ChangePassword`, `Activate`, `Deactivate`, `Lock`, `Unlock`,
`RecordAccessFailure`, `ResetAccessFailures`, `RotateSecurityStamp`.

Invariants:

- Email is globally unique across users and always normalized (ADR-013).
- At least one active credential always exists; secrets are stored hashed or encrypted
  (ADR-014). Password is no longer mandatory (passwordless/passkey-first is allowed).
- A `Disabled` or `Locked` user cannot authenticate.
- A locked user unlocks automatically when `LockoutEndUtc` passes.
- The security stamp changes whenever credentials, status or roles change.
- Role ids in the set are unique.

### Role (aggregate root)

A named collection of permissions. From the consumer's point of view a role is an administration
artifact; the authorization gate is always the permission (ADR-012).

| Member | Kind | Notes |
| --- | --- | --- |
| `Id` | Value object `RoleId` | Strongly typed identity. |
| `Name` | Value object `RoleName` | Unique. |
| `Description` | `string?` | Optional. |
| `Permissions` | Value set `Permission` | Permission value objects. |
| `IsSystem` | `bool` | System roles cannot be renamed or deleted. |

Behaviors: `Create`, `Rename`, `ChangeDescription`, `Grant`, `Revoke`.

Invariants:

- Name is unique.
- Every permission exists in the permission catalog.
- System roles keep their name and cannot be deleted.

### UserSession (aggregate root)

A refresh-token session for a user on a device. Owns the refresh-token rotation chain, which
makes reuse detection a single-aggregate invariant.

| Member | Kind | Notes |
| --- | --- | --- |
| `Id` | Value object `SessionId` | Strongly typed identity. |
| `UserId` | Value object `UserId` | Session owner, referenced by id. |
| `Tokens` | Entity collection `RefreshToken` | Rotation chain. |
| `CreatedOnUtc` | `DateTimeOffset` | Session start. |
| `AbsoluteExpiresOnUtc` | `DateTimeOffset` | Hard maximum lifetime. |
| `RevokedOnUtc` | `DateTimeOffset?` | Set on revocation. |
| `Device` | Value object `SessionMetadata` | User agent / IP fingerprint (minimal, see below). |

Behaviors: `Start`, `Rotate`, `Revoke`, `RevokeChain`.

Invariants:

- Only one token in the chain is active at a time.
- Presenting a rotated (used) token revokes the entire chain (reuse detection).
- A session cannot be used after `AbsoluteExpiresOnUtc` or after revocation.

## Entities

### RefreshToken (owned by UserSession)

| Member | Kind | Notes |
| --- | --- | --- |
| `Id` | Value object `RefreshTokenId` | Strongly typed identity. |
| `Hash` | Value object `RefreshTokenHash` | Hash of the opaque token; plaintext is never stored. |
| `IssuedOnUtc` | `DateTimeOffset` | Issue time. |
| `ExpiresOnUtc` | `DateTimeOffset` | Token expiry (shorter than the session maximum). |
| `UsedOnUtc` | `DateTimeOffset?` | Set when rotated. |
| `RevokedOnUtc` | `DateTimeOffset?` | Set on revocation. |
| `ReplacedByTokenId` | `RefreshTokenId?` | Chain link for audit and reuse detection. |

### PasswordResetToken (owned by User)

| Member | Kind | Notes |
| --- | --- | --- |
| `Id` | Value object `PasswordResetTokenId` | Strongly typed identity. |
| `Hash` | Value object `RefreshTokenHash` | Hash of the opaque reset token; plaintext never stored. |
| `IssuedOnUtc` | `DateTimeOffset` | Issue time. |
| `ExpiresOnUtc` | `DateTimeOffset` | Short expiry (single-use). |
| `UsedOnUtc` | `DateTimeOffset?` | Set when the reset completes. |

Invariants: a user has at most one active reset token; a token can be used once and expires
quickly. The reset token is delivered by Notifications through an integration event; Identity only
issues and validates it.

## Value Objects

| Value object | Represents | Notes |
| --- | --- | --- |
| `UserId` | User identity | Strongly typed id (database-skill). |
| `RoleId` | Role identity | Strongly typed id. |
| `SessionId` | Session identity | Strongly typed id. |
| `RefreshTokenId` | Token identity | Strongly typed id. |
| `PasswordResetTokenId` | Password reset token identity | Strongly typed id. |
| `Email` | Email address | Validated, normalized, structural equality. |
| `PersonName` | Display name | Non-empty, trimmed, max length. |
| `RoleName` | Role name | Non-empty, unique within the aggregate. |
| `PasswordCredential` | Password hash + algorithm + changed-at | Never exposes the plaintext. |
| `PasswordHash` | Hash bytes/base64 + algorithm | Value object. |
| `SecurityStamp` | Opaque random value | Rotated on security-relevant changes. |
| `UserStatus` | Lifecycle status | `Pending`, `Active`, `Disabled`, `Locked`. |
| `Permission` | `resource.action` code + description | Catalog-defined value object. |
| `RefreshTokenHash` | Hashed opaque token | Comparison without plaintext. |
| `RefreshTokenValue` | Plaintext opaque token | Exists only transiently at issuance. |
| `SessionMetadata` | Device/IP fingerprint | Minimal; PII-minimized, retention-aware. |

### Permission is a value object, not an aggregate

Permissions are **platform-defined and immutable**. Modules declare them; Identity seeds them
into a catalog for integrity and querying. Users never create or edit a permission, so a
`Permission` aggregate would be an abstraction without a lifecycle. It is modeled as a value
object referenced by roles.

This supersedes the earlier draft that listed `Permission` as an aggregate and a
`PermissionGranted` domain event.

## Domain Events

Domain events are intra-module and dispatched after commit (ADR-009).

### User

- `UserRegistered`
- `UserEmailChanged`
- `UserNameChanged`
- `UserActivated`
- `UserDeactivated`
- `UserLocked`
- `UserUnlocked`
- `UserPasswordChanged`
- `UserPasswordResetRequested`
- `UserPasswordResetCompleted`
- `RoleAssignedToUser`
- `RoleRemovedFromUser`
- `UserSignedIn`
- `UserSignedOut`

### Role

- `RoleCreated`
- `RoleRenamed`
- `RolePermissionsChanged`
- `RoleDeleted`

### Session

- `SessionStarted`
- `RefreshTokenIssued`
- `RefreshTokenRotated`
- `RefreshTokenRevoked`
- `RefreshTokenReuseDetected`
- `SessionRevoked`

## Cross-Module Communication

Domain events never leave Identity (ADR-009). Cross-module reactions are published as
integration events through the Outbox and exposed in `Identity.Contracts`; see
[module-boundaries.md](module-boundaries.md).

## Persistence Notes

- Auditing and soft delete are applied by the persistence layer, not the aggregates (ADR-010).
- Repositories are aggregate-specific: `IUserRepository`, `IRoleRepository`,
  `IUserSessionRepository` (Rule 6). No `IRepository<T>`.
- Identity owns the `identity` PostgreSQL schema.
