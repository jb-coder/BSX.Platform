# ADR-022 - Account Lockout Hardening

## Status

Accepted

---

## Context

`CredentialAuthenticator` recorded a failed access attempt and saved it through the command's unit
of work. `TransactionBehavior` wraps commands in a transaction and rolls it back when the handler
returns a failed `Result`. A failed login therefore rolled back the incremented
`AccessFailedCount` and any lockout transition, so repeated invalid passwords did not lock the
account. The same applied to tenant discovery, which also uses the authenticator.

Separately, `RefreshTokenCommandHandler` validated the user's existence and security stamp but not
the account status. A locked account holding a valid refresh token could continue refreshing, and
the refresh handler's own revocation writes were also rolled back by the failed command
transaction.

The platform requires:

- durable access-failure counters;
- durable lockout;
- refresh tokens that cannot bypass lockout;
- correct handling of existing sessions;
- fail-closed behaviour.

---

## Decision

### 1. Persist security state outside the command transaction

`CredentialAuthenticator` no longer writes the failure through the command unit of work. It queues
`IAccountSecurityStore.RecordAccessFailureAsync` on `IPostCommandActionQueue`.

`TransactionBehavior` runs the queue **after** the command transaction has been disposed, so the
security write is independent of the command's success or rollback. The queue is flushed with a
non-request token so a client abort cannot suppress the write.

`AccountSecurityStore` uses its own scope and transaction. On PostgreSQL it acquires a
transaction-scoped advisory lock per user so concurrent failures cannot lose an increment.

### 2. Lockout transition

When the configured threshold is reached, the user security state is committed first, in its own
durable transaction:

- `AccessFailedCount` is persisted;
- `Status` becomes `Locked` and `LockoutEndUtc` is set;
- the **security stamp is rotated**.

The security stamp rotation is part of the `User` aggregate (`Lock`, `Unlock` and the lockout branch
of `RecordAccessFailure`).

Session cleanup is a separate, best-effort pass that runs only after the lockout is durable:

- **all active sessions are revoked** with reason `account_locked`.

### 2a. Concurrency isolation of session revocation

Session rows carry an optimistic `Version` token. Saving the lockout and the session revocations in
one `SaveChanges` meant that a concurrent session mutation (`RevokeSessionCommand`, refresh rotation)
could raise `DbUpdateConcurrencyException` and roll back the whole transaction, including the
lockout. That is a lockout bypass.

The store now commits the user security state before touching sessions. The session pass is
independently retried and never propagates a concurrency conflict; if it cannot complete, the
lockout and stamp remain in effect and the stale sessions are rejected by revalidation. User-row
conflicts are retried by re-reading the user and re-applying the failure, so a stamp-rotation
conflict cannot lose the lockout either.

### 3. Session strategy

**Lockout revokes all active sessions and rotates the security stamp.**

Rationale: lockout is a containment response. Keeping a session alive after lockout would let a
compromised session continue. Revoking sessions invalidates access tokens through the authorization
revalidation layer; rotating the stamp invalidates refresh tokens even if a session row were missed.
The cost is a forced re-login after the lockout, which is acceptable for a security lockout.

Administrator unlock clears the lockout and counter, rotates the stamp and does **not** restore
sessions or refresh tokens.

### 4. Refresh protection

`RefreshTokenCommandHandler` rejects any user whose status is not `Active` (`Locked`, `Disabled`,
`Pending`) with `InvalidRefreshToken`. It also rejects a security-stamp mismatch. Rejections queue a
durable session revocation after the failed command transaction, and refresh-token reuse detection
does the same.

### 5. Configuration

`LockoutOptions` (`Identity:Lockout`) controls `MaxFailedAttempts` (default 5) and
`LockoutDurationMinutes` (default 15). The domain defaults remain the fallback.

### 6. Administrator unlock

`UnlockUserCommand` requires `identity.users.manage-status`, clears the lockout and counter, rotates
the stamp and is exposed at `POST /identity/users/{id}/unlock` and in the administration UI.

---

## Alternatives Considered

### Commit the command on failure

Pros

- No additional write path.

Cons

- A failed command would commit partial state. Unsafe for every other command.

### Record the failure before the transaction

Pros

- Simple ordering.

Cons

- The password verification happens inside the handler, after the transaction has started.

### Keep sessions valid on lockout

Pros

- Less user disruption; matches ASP.NET Identity's default cookie behaviour.

Cons

- A stolen session survives the lockout, undermining containment.

### Rotate the stamp without revoking sessions

Pros

- Refresh tokens are invalidated.

Cons

- Access tokens remain usable until expiry and the authorization revalidation would still need the
  status check. Revoking sessions gives immediate, explicit invalidation.

---

## Consequences

### Positive

- Failed logins persist despite the command rollback.
- Lockout persists and is authoritative.
- A concurrent session revocation or update cannot roll back the lockout or the security stamp.
- Refresh tokens cannot bypass lockout.
- Lockout revokes sessions and rotates the stamp.
- Tenant discovery cannot bypass the counter.
- Client aborts cannot suppress the failure write.
- The behaviour is covered by adversarial tests.

### Negative

- Security writes run after the command transaction, in a separate scope. A process crash in the
  narrow post-rollback window can lose one increment; the login still fails.
- Session revocation is best effort. If it keeps conflicting, a session row may stay active
  transiently, but its stale security stamp makes it unusable.
- Lockout can be used to deny access to a known account for the configured duration.
- Administrator unlock forces a fresh sign-in.

---

## Notes

- Referenced by: [account-lockout.md](../security/account-lockout.md),
  [authentication.md](../modules/identity/authentication.md),
  [ADR-017](ADR-017-Token-Invalidation-And-Session-Strategy.md).
- Adversarial tests: `tests/Integration/BSX.Web.IntegrationTests/Security/AccountLockoutTests.cs`
  and `tests/Integration/BSX.Web.IntegrationTests/Security/AccountLockoutConcurrencyTests.cs`.
