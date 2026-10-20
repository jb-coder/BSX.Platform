# Password Reset Delivery (D3)

Password reset is defined by ADR-016: the reset token is random, only its hash is stored, and the
plaintext token is delivered out of band.

## Posture: safe and explicit, never a silent success

By default, no delivery mechanism is configured. The module registers a notifier that reports
`IsDeliveryEnabled == false`. In that state:

- `POST /identity/password-reset/request` returns **`503 Service Unavailable`** with a stable
  problem code (`Identity.PasswordReset.Unavailable`), for every request (known and unknown
  accounts alike) — preserving enumeration-safety.
- No token is persisted and nothing is logged.

This replaces the previous silent no-op that answered `200 OK` even though no email could be sent.

## Integrating a delivery mechanism (Notifications module)

The Notifications module (or the host) provides the production implementation of
`IPasswordResetNotifier`:

```csharp
internal sealed class EmailPasswordResetNotifier : IPasswordResetNotifier
{
    public bool IsDeliveryEnabled => true; // default; set false when the channel is down

    public Task NotifyAsync(string email, string resetToken, DateTimeOffset expiresAtUtc, CancellationToken ct)
        => /* send the email; never persist or log the plaintext token */;
}
```

Register it after `AddIdentityInfrastructure` so it replaces the disabled default:

```csharp
services.RemoveAll<IPasswordResetNotifier>();
services.AddSingleton<IPasswordResetNotifier, EmailPasswordResetNotifier>();
```

`NotifyAsync` is invoked only after the token hash has been persisted, and it **must not throw for
existing accounts** (a difference in behaviour between known and unknown accounts would leak
account existence). Failures must be handled internally or routed through the durable outbox.

## Enumeration-safety guarantees

- Unknown/invalid email and known email return the same result.
- When delivery is disabled, all requests return the same `503`.
- Login and reset responses never reveal whether an account exists.

## ADR-016 and the Outbox

ADR-016 specifies a sensitive `PasswordResetRequestedIntegrationEvent` stored encrypted in the
Outbox for asynchronous delivery. The `IOutboxWriter` seam exists; wiring the sensitive event
(including at-rest encryption of the token) is future work owned by the Notifications module. Until
then, the explicit `503` behaviour above is the safe production default.
