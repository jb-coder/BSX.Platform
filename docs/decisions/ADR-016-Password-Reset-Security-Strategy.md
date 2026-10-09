# ADR-016 - Password Reset Security Strategy

## Status

Accepted

**Amends:** the password-reset flow in `docs/modules/identity/application.md` and
`docs/modules/identity/module-boundaries.md`.

---

## Context

The Principal Architect Review identified a Critical/High security gap: the design publishes a
`UserPasswordResetRequestedIntegrationEvent` so Notifications can email the reset link. If the raw
reset token travels in that event, it is persisted as plaintext in the Outbox `jsonb` payload —
a usable secret at rest in the database.

Requirements:

- No usable reset token stored in Outbox payloads.
- Support email notifications.
- Maintain the Outbox architecture.
- Preserve replay protection.
- Preserve auditability.

---

## Decision

Adopt a **layered strategy: hashed token, sensitive encrypted Outbox payload, and defense in
depth.**

### 1. Token design

- Cryptographically random **256-bit** value, base64url encoded.
- Identity stores **only a SHA-256 hash** (`password_reset_tokens.Hash`). The plaintext is never
  persisted by Identity.
- **TTL: 30 minutes.** A new request invalidates any prior token for the user.
- **Single-use.** Completion marks the token `UsedOnUtc` atomically; reuse is rejected.
- **Invalidated** on password change, credential change and on status/lock changes.
- Optional binding to the requesting device fingerprint to reduce theft value.

### 2. No usable secret at rest in the Outbox

- The `PasswordResetRequestedIntegrationEvent` is a **sensitive event**.
- Sensitive payloads are stored **encrypted at rest** (envelope encryption, AES-256-GCM) using a
  key from the secret store — not alongside the database.
- The Outbox processor **decrypts just in time** immediately before publishing, never logs the
  plaintext, and clears the buffer afterward.
- Non-sensitive events remain plaintext so observability is unaffected.

This satisfies "no usable token stored in Outbox payloads": the payload column holds only
ciphertext.

### 3. Event structure

| Event | Sensitive | Payload |
| --- | --- | --- |
| `PasswordResetRequestedIntegrationEvent` | Yes | `ResetRequestId`, `UserId`, `Email`, `ExpiresOnUtc`, `ResetToken` (the only secret field). |
| `PasswordResetCompletedIntegrationEvent` | No | `ResetRequestId`, `UserId`, `CompletedOnUtc`. |

The event carries the **user email and token** only because the external delivery channel must
reach the user; both fields are inside the encrypted region.

### 4. Notification flow

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant API as Identity.Endpoints
    participant H as RequestPasswordResetHandler
    participant DB as identity schema
    participant OB as Outbox (encrypted payload)
    participant N as Notifications
    participant M as Email provider

    U->>API: POST /identity/auth/password/forgot { email }
    API->>H: RequestPasswordResetCommand
    H->>H: generate token, compute hash
    H->>DB: store hash (TTL 30m), invalidate prior
    H->>OB: PasswordResetRequested (encrypted: token + email)
    H-->>API: 202 Accepted (always; anti-enumeration)
    OB->>N: publish (decrypted just-in-time)
    N->>M: send reset email with link
    M->>U: email
    Note over DB: only the hash is stored; no usable token at rest after publish
```

### 5. Replay protection and expiration

- Single-use enforcement is atomic (row version / conditional update).
- TTL bounds the window; a new request invalidates the previous token.
- Password change or security-stamp rotation invalidates all outstanding tokens.
- Rate limiting per email and per IP on `forgot` and `reset`.

---

## Threat Mitigations

| Threat | Mitigation |
| --- | --- |
| Token interception at rest (DB dump) | Only the hash is stored; Outbox payload is encrypted. |
| Token replay | Single-use + TTL + invalidation on new request/password change. |
| Email enumeration | Always `202 Accepted`; identical response irrespective of account existence. |
| Brute force of token | 256-bit entropy; rate limiting; short TTL. |
| Token leakage in logs | Token never logged; sensitive payload excluded from logs. |
| Fixation | Token generated per request; prior invalidated. |
| Privilege escalation via reset | Reset only sets a credential; authorization unchanged. |
| Key compromise (encryption) | Short TTL + single use limit value; key rotation policy. |

---

## Alternatives Considered

- **Plaintext token in Outbox (current)** — rejected: usable secret at rest.
- **Out-of-band notification port (no Outbox for this flow)** — viable and stronger at rest, but
  loses durable delivery and couples Identity to a delivery adapter. Retained as a configurable
  option for maximum-isolation deployments.
- **Consumer callback to fetch the token** — rejected: Identity stores only a hash and cannot
  reconstruct the plaintext.
- **Signed self-contained token with no stored state** — reduces storage but still places a usable
  secret in the event; requires encryption anyway and complicates single-use enforcement.

---

## Consequences

### Positive

- No usable reset token at rest in the Outbox.
- Event-driven email delivery and Outbox reliability are preserved.
- Strong replay protection and auditability.
- Sensitive-payload capability is reusable for future secret-bearing events.

### Negative

- Introduces envelope encryption and key management to the Outbox.
- Sensitive events are not inspectable in plaintext, slightly reducing operational visibility.

---

## Future Considerations

- Extend the sensitive-payload mechanism to API key issuance and other secret-bearing events.
- KMS/HSM-backed keys and automatic rotation.
- Consider the out-of-band port for regulated, maximum-isolation deployments.
- Add breached-password checks and step-up authentication to the reset flow.
