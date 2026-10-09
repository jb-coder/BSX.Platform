# Identity - JWT Strategy

Design of the access token. Refresh tokens are covered in
[refresh-token-strategy.md](refresh-token-strategy.md).

---

## Token Choice

- **Access token:** JWT (JSON Web Token), stateless, short-lived.
- **Algorithm:** asymmetric signing (RS256 or ES256). Asymmetric keys let multiple components
  validate tokens with the public key only; the private key never leaves Identity.
- **Key identifier:** every token carries `kid`; the matching public key is published via a JWKS
  endpoint.

Stateless access tokens were chosen over reference tokens to avoid a validation round-trip per
request (Rule 10: choose the simpler solution). The trade-off is documented under
[Revocation](#revocation).

---

## Claims

| Claim | Value | Purpose |
| --- | --- | --- |
| `iss` | Identity issuer | Validated. |
| `aud` | Platform API audience | Validated. |
| `sub` | `UserId` | Stable caller identity. |
| `jti` | Unique token id | Correlation / emergency revocation. |
| `iat` | Issued at | Standard. |
| `nbf` | Not before | Standard. |
| `exp` | Expiry | Short lifetime. |
| `email` | User email | Convenience; not authoritative for authorization. |
| `name` | Display name | UI convenience. |
| `role` | Role names (0..n) | Optional; administration/UI only. |
| `permission` | Effective permissions (0..n) | Authorization gate (ADR-012). |
| `sstamp` | Security stamp | Detects security-relevant changes. |
| `tenant` | Reserved | Multi-tenancy; unused until that phase. |

The reserved permission claim type is `permission` (ADR-012). Authorization is evaluated against
`permission`, never against `role`.

---

## Lifetime

- Access token lifetime: **short** (default 15 minutes, configurable per environment).
- Short lifetime bounds the window in which a revoked permission remains usable.
- No sliding renewal of the access token itself; clients obtain a new one by refreshing.

---

## Signing Keys

- Keys are asymmetric; the private key lives only in the secret store
  (environment/user-secrets/Vault). Keys are never committed.
- Rotation is supported through multiple active keys distinguished by `kid`.
- Public keys are exposed at `GET /identity/.well-known/jwks.json`.
- Validation uses the published key set; on unknown `kid`, validators refresh the key set.

---

## Validation

Token validation configuration:

- Validate issuer, audience, lifetime and signature.
- Explicit algorithm allowlist (asymmetric only; `none` is rejected).
- Small clock skew (default 30 seconds).
- Reject tokens without `sub` or `permission` where required.

---

## Revocation

Stateless tokens cannot be individually revoked without a server lookup. Identity mitigates this:

- **Short lifetime** limits exposure.
- **Security stamp** (`sstamp`) is validated **at refresh time**, not per request. A rotated stamp
  fails the refresh and forces re-authentication (ADR-017). Access tokens remain stateless; there
  is no per-request database or cache lookup.
- **Sessions** are revoked for refresh tokens (see refresh strategy), forcing re-authentication.
- A deny-list keyed by `jti` is reserved for emergency use only, to avoid per-request lookups.

---

## Transport and Storage

- TLS is mandatory. The token is sent as `Authorization: Bearer <token>`.
- Clients store the access token in memory only; it is never persisted in `localStorage`.
- The refresh token follows [refresh-token-strategy.md](refresh-token-strategy.md).
- The Blazor Web App host chooses its transport (Bearer for APIs, cookie for the interactive UI);
  Identity issues the tokens and does not dictate the host's authentication scheme.

---

## Provider Scope

Identity is a first-party token issuer, **not** a general OIDC/OAuth2 provider in this phase.
External identity providers, SSO and federation would require a dedicated ADR.

---

## Security Considerations

- Claims contain no sensitive data; the token is signed, not encrypted.
- Permissions are the only authorization claims; role claims are cosmetic.
- Large permission sets grow the token; keep permissions coarse-grained and the lifetime short.
  If size becomes a problem, revisit with token introspection via a new ADR.
- Clock skew is small; servers must be time-synchronized.
