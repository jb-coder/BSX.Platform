# Identity - Design Amendments

This document records how ADR-013 through ADR-018 amend the original Identity design. Where this
document and an earlier Identity document disagree, **this document (and the ADR) wins**.

---

## ADR-013 - Multi-Tenant Identity Strategy

Amends [domain-model.md](domain-model.md), [permissions-model.md](permissions-model.md),
[jwt-strategy.md](jwt-strategy.md), [application.md](application.md), [api-surface.md](api-surface.md).

- Users are **global**; `TenantMembership` is a new aggregate linking user ↔ tenant.
- Roles are **tenant-scoped**: `Role.TenantId` is nullable (`null` = platform/system role).
- Role assignment lives on `TenantMembership` (user roles) and `ServiceAccount` (machine roles).
- Effective permissions are resolved **per tenant** (membership roles ∪ platform roles).
- Access tokens are **tenant-bound** (`tenant`, `membership` claims); tenant switching re-issues a
  token without re-authenticating.
- `User.Email` is **globally unique**.
- New commands/queries: switch tenant, list my tenants; membership management (invite, accept,
  suspend).

---

## ADR-014 - Credential Model Strategy

Amends [domain-model.md](domain-model.md).

- Introduce a **`Principal`** abstraction: `User` (human) and `ServiceAccount` (machine).
- **Human credentials** are **child entities of `User`**: `PasswordCredential`, `TotpCredential`,
  `RecoveryCodeCredential`, `WebAuthnCredential`, `ExternalProviderCredential`.
- **Machine credentials** (`ApiKey`) are a **separate aggregate** referenced by a principal.
- `ServiceAccount` is a separate aggregate with no password/MFA/passkeys.
- Replaces the single `PasswordCredential` value object on `User`.
- MFA is modeled as additional credentials plus an `MfaPolicy`.

---

## ADR-015 - Contracts Abstraction Strategy

Amends [module-boundaries.md](module-boundaries.md).

- New platform project **`BSX.Contracts`** owns `IIntegrationEvent`, the integration-event base and
  shared identifiers (`UserId`, `RoleId`, `TenantId`, `PrincipalId`).
- `Identity.Contracts` references `BSX.Contracts`, **not** `BSX.BuildingBlocks`.
- Update the contracts dependency row and shared-id ownership.

---

## ADR-016 - Password Reset Security Strategy

Amends [application.md](application.md), [refresh-token-strategy.md](refresh-token-strategy.md).

- Reset tokens: 256-bit random, **only the hash is stored**, TTL 30 minutes, single-use,
  invalidate-on-new-request, invalidated on password/status change.
- `PasswordResetRequestedIntegrationEvent` is a **sensitive event** stored **encrypted** at rest in
  the Outbox; the plaintext token never sits in the database.
- Notifications consumes the event and sends the email.
- Reset completion invalidates all sessions and raises a non-sensitive
  `PasswordResetCompletedIntegrationEvent`.

---

## ADR-017 - Token Invalidation, Security Stamp and Session Strategy

Amends [jwt-strategy.md](jwt-strategy.md), [refresh-token-strategy.md](refresh-token-strategy.md).

- Access tokens are stateless and short-lived; **no per-request security-stamp lookup**.
- `SecurityStamp` is validated **at refresh time**; it rotates on password/credential/status changes
  and on tenant removal.
- Revocation unit is the **session**; emergency `jti` deny-list only for incidents.
- Sessions: absolute max 30 days, idle timeout 7 days, cap 10 per principal, tenant binding.
- Refresh rotation gains a **30-second grace window** for the immediately previous token.
- **Ordinary** role/permission changes do **not** revoke sessions; they propagate within the token
  TTL.

---

## ADR-018 - Role Lifecycle and Permission Cache Strategy

Amends [permissions-model.md](permissions-model.md).

- Role deletion is **blocked while assigned** (reassign first), system roles are immutable, and
  deletion is **soft**.
- System roles use **selectors**: `Administrator` = all permissions, `Auditor` = all read
  permissions; `SynchronizePermissions` reconciles them as the catalog grows.
- Permission cache invalidation: token TTL for embedded claims; `RolePermissionsChanged` and
  `UserRolesChanged` integration events for external caches.

---

## Result

With these amendments, the Identity module is implementation-ready and supports enterprise and
SaaS deployment, multi-company usage, and future MFA, passkeys, SSO, API keys and service accounts
without a redesign.
