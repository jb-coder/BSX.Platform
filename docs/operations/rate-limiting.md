# Rate Limiting (D4)

Identity rate-limits authentication-sensitive operations before doing expensive work, and fails
closed (a denied request does not proceed).

## What is limited

| Operation | Partition key | Configured via |
| --- | --- | --- |
| Login | `login:{email}` (lower-cased) | `RateLimitOptions.Login` |
| Refresh | `refresh:{tokenHash}` | `RateLimitOptions.Refresh` |
| Password reset request | `password-reset:{email}` | `RateLimitOptions.PasswordReset` |

When a limit is exceeded the operation returns a conflict/unavailable result mapped to
`409 Conflict` (`Identity.Authentication.TooManyRequests`).

## Implementation

The default `IRateLimiter` is `InMemoryRateLimiter`, a fixed-window counter held in process memory.

## Behaviour by topology

- **Single instance:** limits are accurate and fully effective.
- **Multiple instances (HA):** limits are **per instance**. With N instances the effective
  throughput is up to N× the configured limit, and a restart resets the counters.

## Distributed limits (HA)

For cluster-wide limits, implement `IRateLimiter` against a shared store and register it after
`AddIdentityInfrastructure`:

```csharp
services.Replace(ServiceDescriptor.Singleton<IRateLimiter, RedisRateLimiter>());
```

The `IRateLimiter.Acquire(partitionKey, policy)` contract is small and designed for this.

## Operational notes

- Losing rate-limit state on restart is acceptable for login/reset (fail-closed on the next burst).
- Rate limiting complements, and does not replace, account lockout (ADR-014) and password-reset
  single-use tokens (ADR-016).
