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
| `Identity:Database:VerifySchema` | No | `false` | Enables the Identity schema readiness check. |
| `ConnectionStrings:Crm` | No | Identity connection | PostgreSQL for the CRM module. When omitted, CRM uses `ConnectionStrings:Identity` and the `crm` schema. |
| `Crm:Database:VerifySchema` | No | `false` | Enables the CRM schema readiness check. |
| `ConnectionStrings:Sales` | No | Identity connection | PostgreSQL for the Sales module. When omitted, Sales uses `ConnectionStrings:Identity` and the `sales` schema. |
| `Sales:Database:VerifySchema` | No | `false` | Enables the Sales schema readiness check. |
| `Identity:Permissions:SynchronizeOnStartup` | No | `true` | Seeds/synchronizes system roles from the registered module permission catalogs. Missing schema is logged and skipped. |
| `AllowedHosts` | Yes | `localhost;127.0.0.1` | Set to your public host name(s) in production. |
| `OpenTelemetry:OtlpEndpoint` | No | `""` | Enables OTLP export when set. |
| `Outbox:Enabled` | No | `true` | Writes domain events to each module outbox. |
| `Outbox:ProcessingEnabled` | No | `true` | Runs the background crash-recovery loop. |
| `Outbox:IntegrationPublishingEnabled` | No | `false` | Publishes integration rows through `IEventBus`; enable only with a broker. |
| `Outbox:PollInterval` | No | `00:00:05` | Recovery poll interval. |
| `Outbox:BatchSize` | No | `100` | Rows per module per poll. |
| `Outbox:MaxRetryCount` | No | `10` | Failed attempts before dead-lettering. |
| `Outbox:BaseBackoff` | No | `00:00:05` | Exponential retry base. |
| `Outbox:StaleProcessingAfter` | No | `00:05:00` | Requeue claims older than this after a crash. |

## Security-sensitive defaults

- **`AllowedHosts`** is restricted by default. Set it to the public host name(s) so Host-header
  filtering rejects unexpected hosts. Do not use `*` in production.
- **Connection strings.** There is no implicit production default. Outside Development the host
  fails fast when `ConnectionStrings:Identity` is missing; `ConnectionStrings:Crm` and
  `ConnectionStrings:Sales` are optional and fall back to the Identity database. The Development
  fallbacks use local credentials only.
- **JWT keys.** Outside Development the host fails fast when signing keys are missing
  ([jwt-key-management.md](jwt-key-management.md)).
- **HSTS / HTTPS redirection** are enabled outside Development.

## Environment variable mapping

Double underscores map to nested keys:

```
ConnectionStrings__Identity=Host=db;Database=bsx_identity;Username=bsx;Password=***;SSL Mode=Require
ConnectionStrings__Crm=Host=db;Database=bsx_crm;Username=bsx;Password=***;SSL Mode=Require
ConnectionStrings__Sales=Host=db;Database=bsx_sales;Username=bsx;Password=***;SSL Mode=Require
Identity__Jwt__SigningKeys__0__Kid=2026-01
Identity__Jwt__SigningKeys__0__PrivateKeyPem=-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----
Identity__Database__VerifySchema=true
Identity__Permissions__SynchronizeOnStartup=true
Crm__Database__VerifySchema=true
Sales__Database__VerifySchema=true
AllowedHosts=identity.example.com
```

Omit `ConnectionStrings__Crm` and/or `ConnectionStrings__Sales` to share the Identity database
(each module uses its own `crm`/`sales` schema).

## Configuration validation

`JwtOptionsConfiguration.Apply` (startup) and the host's connection-string check reject invalid or
incomplete configuration before the host begins serving. Failures are logged and the process exits
non-zero.
