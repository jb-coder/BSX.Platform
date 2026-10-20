# JWT Signing Key Management (D1)

Identity issues RS256 access tokens (ADR-017). Signing keys are read from `Identity:Jwt:SigningKeys`.

## Configuration

```json
{
  "Identity": {
    "Jwt": {
      "Issuer": "bsx-platform",
      "Audience": "bsx-platform",
      "AccessTokenLifetimeMinutes": 15,
      "SigningKeys": [
        { "Kid": "2026-01", "PrivateKeyPem": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----" }
      ]
    }
  }
}
```

Supply the private key through your secret store (environment variables, Key Vault, Kubernetes
secret) — never commit it. The first key in `SigningKeys` is the active signing key; all keys are
exposed as validation keys (JWKS-ready).

## Startup validation

`JwtOptionsConfiguration.Apply` validates the section at startup:

- `Issuer`, `Audience` and a positive `AccessTokenLifetimeMinutes` are required.
- Every signing key must define a `PrivateKeyPem`.
- **Development**: if no key is configured, an ephemeral in-memory key is generated for convenience.
- **Non-Development**: if no key is configured, the host **fails fast** with an
  `InvalidOperationException` — it will not start with an ephemeral key.

> An ephemeral key is generated per process. It is lost on restart and cannot be shared, so it must
> never be used in production.

## Rotation strategy

Access tokens are short-lived and stateless, so key rotation is a rolling operation:

1. **Add** the new key (new `Kid`) to `SigningKeys`. Keep the previous key in the list.
2. Roll out the new configuration to all instances. New tokens are signed with the first key
   (the new key); older tokens remain valid because the previous key is still published for
   validation.
3. Wait at least one access-token lifetime (default 15 minutes) — plus clock skew — so that all
   tokens signed with the old key have expired.
4. **Remove** the old key from `SigningKeys` and roll out the configuration again.

Emergency rotation (suspected key compromise): rotate immediately and invalidate sessions by
rotating security stamps or revoking sessions; the ADR-017 `jti` deny-list is the mechanism for
targeted emergency revocation during an incident.

## Multi-instance requirement

All instances must share the same key material (D4). In HA deployments the key must come from a
shared secret store, not from per-instance configuration.
