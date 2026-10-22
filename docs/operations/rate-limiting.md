# Rate Limiting

The platform exposes a provider-neutral rate limiting seam in
`BSX.BuildingBlocks.RateLimiting`. Identity uses the same concepts for its authentication
operations (login, refresh, password reset).

## Platform abstraction

| Type | Responsibility |
| --- | --- |
| `RateLimitPolicy` | Named fixed window: `Name`, `PermitLimit`, `Window`. |
| `RateLimitRequest` | Partition key plus policy. |
| `RateLimitDecision` | `IsAllowed`, `Limit`, `Remaining`, `RetryAfter`, `ResetAtUtc`. |
| `IRateLimitStore` | Acquire a permit. |
| `IDistributedRateLimitStore` | Marker for a cluster-wide implementation. |
| `InMemoryRateLimitStore` | Default single-instance fixed-window implementation. |

`AddBuildingBlocks()` registers `InMemoryRateLimitStore` as the default. Callers depend only on
`IRateLimitStore`, so moving to a shared store requires a single registration change.

## Behaviour by topology

- **Single instance:** limits are accurate and fully effective.
- **Multiple instances (HA):** the in-memory store is **per process**. With N instances the
  effective throughput is up to N x the configured limit and a restart resets counters. This is
  fail-closed (a request is never admitted because state was lost) but not cluster-accurate.

## Distributed limits (HA)

Implement `IDistributedRateLimitStore` against a shared store (for example Redis) and replace the
default after `AddBuildingBlocks`:

```csharp
services.Replace(ServiceDescriptor.Singleton<IRateLimitStore, RedisRateLimitStore>());
```

No provider is mandated yet. The contract is intentionally small so a Redis, SQL or gateway-backed
implementation can be added without touching modules. Use a stable partition key (normalized email,
token hash, tenant + principal) and a policy name to avoid collisions.

## Identity authentication limits

Identity rate-limits authentication-sensitive operations before doing expensive work and fails
closed:

| Operation | Partition key | Configured via |
| --- | --- | --- |
| Login | `login:{email}` (lower-cased) | `RateLimitOptions.Login` |
| Refresh | `refresh:{tokenHash}` | `RateLimitOptions.Refresh` |
| Password reset request | `password-reset:{email}` | `RateLimitOptions.PasswordReset` |

When a limit is exceeded the operation returns `409 Conflict`
(`Identity.Authentication.TooManyRequests`).

## Operational notes

- Rate limiting complements account lockout (ADR-014) and single-use reset tokens (ADR-016).
- CRM and Sales endpoints are authenticated and permissioned but not individually throttled; add a
  policy at the gateway or a module endpoint filter when needed.
- For HA, complete the shared-store prerequisites in
  [deployment-ha.md](deployment-ha.md).
