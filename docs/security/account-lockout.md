# Account Lockout and Refresh-Token Hardening

This document describes the durable account-lockout model, the session and refresh-token
behaviour around lockout, and the guarantees the platform provides. It is the operational companion
to [ADR-022](../decisions/ADR-022-Account-Lockout-Hardening.md).

---

## Threat model

### Assets

- User credentials (password hashes).
- Active sessions and refresh tokens.
- Tenant data reachable through an authenticated session.

### Adversary

- An unauthenticated attacker performing online password guessing or credential stuffing.
- A holder of a valid refresh token whose account has been locked.
- An attacker who deliberately aborts a failed login to avoid the failure counter.

### In-scope threats

| Threat | Mitigation |
| --- | --- |
| Unlimited password guessing | Durable failure counter + lockout + rate limiting |
| Lockout bypass through command rollback | Failure persisted outside the command transaction |
| Lockout bypass through tenant discovery | Discovery uses the same authenticator and durable counter |
| Refresh token bypass after lockout | Refresh rejects non-active accounts and revokes the session |
| Existing session survives lockout | Lockout revokes all active sessions and rotates the security stamp |
| Client abort skips the failure write | Post-command security actions run with a non-request token |
| Account enumeration | Locked, disabled and pending accounts all return `InvalidCredentials` |

### Out of scope

- Offline attacks against stolen password hashes.
- Distributed credential-stuffing protection beyond the rate limiter.
- Denial of service caused by deliberately locking a known account.

---

## Lockout model

### Durable failure counter

`CredentialAuthenticator` does **not** write the failure inside the login command transaction.
Instead it queues an `IAccountSecurityStore.RecordAccessFailureAsync` action through
`IPostCommandActionQueue`. `TransactionBehavior` runs the queue **after** the command transaction
has been disposed, so:

- a failed login Result cannot roll back the failure counter;
- the same protection applies to tenant discovery;
- the client cannot cancel the write by aborting the request (the queue uses a non-request token).

The store loads the user, applies the domain transition and saves in its own scope/transaction. On
PostgreSQL it acquires a transaction-scoped advisory lock per user so concurrent failures cannot
lose an increment. If a concurrent writer changes the user row, the store re-reads the user and
re-applies the failure on a bounded retry; the rolled-back increment is never double-counted.

### Thresholds

| Setting | Default | Configuration |
| --- | --- | --- |
| Consecutive failures before lockout | 5 | `Identity:Lockout:MaxFailedAttempts` |
| Lockout duration | 15 minutes | `Identity:Lockout:LockoutDurationMinutes` |

The values are read through `LockoutOptions`; the domain defaults
(`User.DefaultMaxAccessFailedCount`, `User.DefaultLockoutDuration`) remain the fallback.

### Lockout transition

When the threshold is reached, the user security state is committed **first**, in its own durable
transaction:

1. `AccessFailedCount` is persisted.
2. `Status` becomes `Locked` and `LockoutEndUtc` is set.
3. The **security stamp is rotated**.
4. A `UserLocked` domain event is recorded (outbox).

Session cleanup is a **separate, best-effort pass** that runs only after the lockout is durable:

5. **All active sessions for the user are revoked** with reason `account_locked`.

Lockout durability never depends on session revocation. A concurrency conflict while revoking a
session cannot roll back the counter, lockout or stamp. If cleanup cannot complete, the sessions
retain the pre-lockout security stamp and are rejected by authorization revalidation and refresh.

### Automatic unlock

A locked account is rejected **before password verification** while the lockout window is active.
The lockout is not extended by further attempts. Once `LockoutEndUtc` has passed, the next login
attempt unlocks the account (clears the counter and lockout, rotates the security stamp) and
proceeds to password verification. A wrong password after expiry records a new failure and
re-locks at the threshold.

### Administrator unlock

`UnlockUserCommand` requires `identity.users.manage-status` (platform scope). It:

- clears `LockoutEndUtc` and `AccessFailedCount`;
- sets the account back to `Active` (or `Pending` when it has no credential);
- rotates the security stamp.

Unlock **does not restore** sessions or refresh tokens. The user must sign in again. The
administration UI exposes an **Unlock user** action for locked accounts and the API exposes
`POST /identity/users/{id}/unlock`.

---

## Session behaviour on lockout

**Chosen strategy: revoke all active sessions and rotate the security stamp.**

Justification:

- Lockout is a containment response to suspected credential compromise. Leaving an existing session
  alive would let an attacker with a stolen session continue operating after the account is locked.
- Revoking sessions invalidates access tokens through the authorization revalidation layer
  (session status is checked before every permission-gated operation and by circuit/cookie
  revalidation).
- Rotating the security stamp invalidates refresh tokens even if a session row were missed.
- The cost is that a legitimate user must sign in again after the lockout. That is acceptable for a
  security lockout; the alternative (sessions remain valid) undermines containment.

The authorization revalidation strategy ([authorization-revalidation.md](authorization-revalidation.md))
denies operations for locked accounts and for sessions whose security stamp no longer matches.

---

## Refresh-token behaviour

`RefreshTokenCommandHandler` validates, in order:

1. The refresh token exists and the session is usable.
2. The user is `Active`. `Locked`, `Disabled` and `Pending` are rejected with
   `InvalidRefreshToken`.
3. The session security stamp matches the user's current stamp.
4. The refresh chain rotation succeeds (including reuse detection).

On rejection:

- the session is revoked durably through `IAccountSecurityStore.RevokeSessionAsync`, queued after
  the failed command transaction;
- reuse detection also persists the revocation after the rollback;
- no access token is issued.

A locked account therefore cannot refresh. After an administrator unlock, the pre-lockout refresh
token remains unusable because the session was revoked and the security stamp was rotated.

---

## Fail-closed behaviour

- If the account-security store cannot persist a failure, the queue action throws and the request
  fails; access is not granted.
- If authentication state cannot be verified, the login/refresh path returns a failure.
- Locked, disabled and pending accounts all return `InvalidCredentials` to avoid enumeration.
- Authorization revalidation denies operations when the user is not active or the session is not
  valid.

---

## Security guarantees

1. A failed password attempt increments a durable counter, even though the login command returns a
   failed Result and its transaction rolls back.
2. Reaching the threshold persists `Locked` and `LockoutEndUtc`.
3. A locked account cannot sign in, even with the correct password, while the lockout is active.
4. A locked account cannot refresh.
5. Lockout revokes all active sessions and rotates the security stamp.
6. A concurrency conflict during session revocation cannot roll back the lockout, the counter or the
   stamp; the lockout is durable before cleanup starts.
7. Administrator unlock clears the lockout but does not revive sessions or refresh tokens.
8. Tenant discovery cannot bypass the failure counter.
9. Client aborts cannot skip the failure write.
10. No account-status disclosure through login errors.

---

## Limitations

- **Lockout is a denial-of-service lever.** An attacker who knows an email can lock the account for
  the configured duration. Rate limiting and monitoring mitigate this; the duration should be tuned
  per deployment.
- **The rate limiter is process-local.** The durable lockout is the cross-instance control; the
  in-memory rate limiter is a secondary defense.
- **Clock trust.** Lockout expiry uses the configured `TimeProvider`; deployments must keep server
  clocks synchronized.
- **Concurrent failures** are serialized with a PostgreSQL advisory lock. On the SQLite test
  provider the write is serialized by the database.
- **Session revocation is best effort.** The lockout and stamp rotation are committed before cleanup.
  If a session revocation keeps conflicting, the session row may stay active transiently; it is still
  rejected because its security stamp no longer matches the user's.
- **Failure persistence occurs after the command transaction.** A process crash in the narrow window
  between rollback and the post-command write can lose one increment; the login still fails. This is
  the deliberate trade-off that keeps the write independent of the command transaction.

---

## Verification

Adversarial coverage:

- `tests/Integration/BSX.Web.IntegrationTests/Security/AccountLockoutTests.cs`
  - failed login persists the counter after command rollback;
  - tenant discovery also persists failures;
  - threshold persists lockout, rotates the stamp and revokes sessions;
  - locked login is rejected without extending the lockout;
  - locked refresh is rejected and the session is revoked;
  - disabled refresh is rejected and the session is revoked;
  - administrator unlock clears the lockout and requires a new sign-in;
  - lockout expiry allows sign-in and clears the counter.
- `tests/Integration/BSX.Web.IntegrationTests/Security/AccountLockoutConcurrencyTests.cs`
  - a concurrent session revoke does not roll back the lockout;
  - a concurrent session update does not roll back the lockout;
  - a user-row/security-stamp conflict is retried and the lockout persists;
  - a session that remains active transiently is still rejected by authorization revalidation;
  - multiple active sessions are all revoked even when one conflicts;
  - the real login threshold path still locks under a concurrent session revoke.
- Domain tests cover `IsLockedOut`, stamp rotation on lock/unlock/lockout.
- Handler tests cover failure recording, locked rejection and refresh revocation paths.
