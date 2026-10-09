# Identity - Refresh Token Strategy

Refresh tokens extend a session without re-entering credentials, with rotation and reuse
detection. They are owned by the `UserSession` aggregate
([domain-model.md](domain-model.md)).

---

## Token Format

- **Opaque**, not a JWT: high-entropy random value (at least 256 bits), base64url encoded.
- The plaintext value (`RefreshTokenValue`) exists only transiently at issuance and in the
  refresh request. Only its hash is stored.
- Opaque tokens cannot be introspected by clients, which reduces the attack surface.

---

## Storage

- Store a cryptographic hash (`RefreshTokenHash`) in `identity.refresh_tokens`.
- Because the token is already high-entropy, a fast hash (for example SHA-256) is sufficient and
  avoids per-request key stretching. Hashing prevents plaintext leakage from a database dump.
- Never log or emit the plaintext value.
- Session metadata (device/user-agent fingerprint, optional IP) is minimal and retention-aware
  (PII minimization).

---

## Lifecycle

```text
Session.Start ──► RefreshToken #1 (active)
                       │ refresh
                       ▼
                  RefreshToken #2 (active)   #1 marked Used
                       │ refresh
                       ▼
                  RefreshToken #3 (active)   #2 marked Used
```

- **Issuance:** logging in starts a session and issues the first refresh token.
- **Rotation:** every successful refresh issues a new token and marks the presented token `Used`,
  linking `ReplacedByTokenId`.
- **Reuse detection:** presenting a `Used` or `Revoked` token means the chain has leaked. Identity
  revokes the entire session (`RevokeChain`) and raises `RefreshTokenReuseDetected` plus
  `SessionRevoked`.
- **Revocation:** the session is revoked; all tokens become unusable.

---

## Expiry

| Limit | Default | Rationale |
| --- | --- | --- |
| Token expiry | 7 days | Bounds an unused token's lifetime. |
| Session absolute maximum | 30 days | Hard ceiling; re-authentication is required after it. |

- Active use extends the session up to the absolute maximum (sliding within a hard ceiling).
- After the absolute maximum, the user must authenticate again.

---

## Revocation Triggers

| Trigger | Scope |
| --- | --- |
| Logout | Current session. |
| Logout all | All sessions of the user. |
| Password change or reset | All sessions of the user. |
| User deactivated or locked | All sessions of the user. |
| Credential change (MFA, passkey, provider) | All sessions of the user (ADR-014). |
| Removed from a tenant | The removed tenant's sessions. |
| Role/permission change | **No revocation** (ADR-017/ADR-018); propagates within token TTL. |
| Reuse detected | The affected session only. |
| Administrative action | Targeted session or all. |
| Security stamp rotation | Tokens with a stale `sstamp` are rejected. |

---

## Concurrency

- Concurrent refreshes of the same token are possible. The rotation update must be atomic
  (optimistic concurrency / row version).
- The winner issues the new token; the immediately previous token is accepted for a **30-second
  grace window** to absorb legitimate races (ADR-017). Beyond the grace, presenting a used token
  triggers reuse detection and the session is revoked.
- Clients must serialize refresh calls (single-flight) to avoid graceful races.
- Sessions are capped (default 10 per principal; oldest evicted), with an idle timeout (7 days)
  and an absolute maximum (30 days) (ADR-017).

---

## Transport

- The refresh token is returned once in the authentication response.
- Recommended storage: `HttpOnly`, `Secure`, `SameSite=Strict` cookie for browser clients, or the
  platform secure store for non-browser clients. It is never stored in `localStorage`.
- The refresh endpoint is `POST /identity/auth/refresh` (Anonymous; the token itself is the
  credential). See [api-surface.md](api-surface.md).

---

## Operational Rules

- Rate-limit login and refresh endpoints.
- Purge expired and revoked tokens on a schedule; hard delete is permitted here because tokens are
  infrastructure data (ADR-010).
- Emit metrics for issued, rotated, revoked and reused tokens.
- Treat refresh-token reuse signals as security events and alert on them.
