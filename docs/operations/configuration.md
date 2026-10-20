# Configuration Reference (D7)

Configuration is layered: `appsettings.json` → `appsettings.{Environment}.json` → environment
variables → command line/secrets. Deployment-specific and secret values must come from the
environment or a secret store, never from committed files.

## Identity settings

| Key | Required | Default | Notes |
| --- | --- | --- | --- |
| `ConnectionStrings:Identity` | Yes (non-Dev) | — | PostgreSQL. Missing outside Development → startup fails. |
| `Identity:Jwt:Issuer` | Yes | `bsx-platform` | Validated at startup. |
| `Identity:Jwt:Audience` | Yes | `bsx-platform` | Validated at startup. |
| `Identity:Jwt:AccessTokenLifetimeMinutes` | No | `15` | Must be > 0. |
| `Identity:Jwt:SigningKeys:0:Kid` | Yes (non-Dev) | — | Key identifier. |
| `Identity:Jwt:SigningKeys:0:PrivateKeyPem` | Yes (non-Dev) | — | RSA private key (PKCS#8 PEM). Missing outside Development → startup fails. |
| `Identity:Database:ApplyMigrationsOnStartup` | No | `false` | Single-instance only. |
| `Identity:Database:VerifySchema` | No | `false` | Enables the schema readiness check. |
| `AllowedHosts` | Yes | `localhost;127.0.0.1` | Set to your public host name(s) in production. |
| `OpenTelemetry:OtlpEndpoint` | No | `""` | Enables OTLP export when set. |

## Security-sensitive defaults

- **`AllowedHosts`** is restricted by default. Set it to the public host name(s) so Host-header
  filtering rejects unexpected hosts. Do not use `*` in production.
- **Connection string.** There is no implicit production default. Outside Development the host
  fails fast when `ConnectionStrings:Identity` is missing; the Development fallback uses local
  credentials only.
- **JWT keys.** Outside Development the host fails fast when signing keys are missing
  ([jwt-key-management.md](jwt-key-management.md)).
- **HSTS / HTTPS redirection** are enabled outside Development.

## Environment variable mapping

Double underscores map to nested keys:

```
ConnectionStrings__Identity=Host=db;Database=bsx_identity;Username=bsx;Password=***;SSL Mode=Require
Identity__Jwt__SigningKeys__0__Kid=2026-01
Identity__Jwt__SigningKeys__0__PrivateKeyPem=-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----
Identity__Database__VerifySchema=true
AllowedHosts=identity.example.com
```

## Configuration validation

`JwtOptionsConfiguration.Apply` (startup) and the host's connection-string check reject invalid or
incomplete configuration before the host begins serving. Failures are logged and the process exits
non-zero.
