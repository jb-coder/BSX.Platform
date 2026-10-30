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
| `Demo:SeedIdentity` | No | `false` | Development-only demo administrator and workspace bootstrap. Ignored outside Development. |
| `Demo:SeedSampleData` | No | `false` | Development-only, idempotent CRM, Sales and invite-ready user sample data. Requires the demo administrator. Ignored outside Development. |

## Deployment hardening settings

| Key | Required | Default | Notes |
| --- | --- | --- | --- |
| `DataProtection:ApplicationName` | Recommended | `BSX.Platform` | Must be identical on every instance that shares cookies. |
| `DataProtection:KeysPath` | Yes (non-Dev) | - | Persistent, writable, shared directory. Missing outside Development fails startup. Development defaults to `<content-root>/.data-protection-keys`. |
| `ReverseProxy:Enabled` | No | `true` | Enables forwarded-header processing. |
| `ReverseProxy:RedirectHttpToHttps` | No | `true` | Application-level HTTP→HTTPS redirect. Disable only when the proxy owns the redirect. |
| `ReverseProxy:ForwardLimit` | No | `1` | Number of trusted proxy hops that append forwarded headers. |
| `ReverseProxy:KnownProxies` | No | `[]` | Trusted proxy IPs. Empty keeps the framework's loopback-only defaults. |
| `ReverseProxy:KnownNetworks` | No | `[]` | Trusted proxy CIDR ranges (for example `10.0.0.0/8`). |
| `Web:Cookies:SecurePolicy` | No | `Always` | `Always`, `SameAsRequest` or `None`. Development uses `SameAsRequest` when the section is absent. |
| `Web:Cookies:AuthSameSite` | No | `Lax` | Authentication cookie SameSite policy. |
| `Web:Cookies:RefreshSameSite` | No | `Strict` | Refresh-token cookie SameSite policy. |
| `Web:Cookies:AntiforgerySameSite` | No | `Strict` | Antiforgery cookie SameSite policy. |
| `Web:Cookies:Domain` | No | `null` | Optional cookie domain; leave empty for host-only cookies. |
| `Hsts:MaxAge` | No | `365.00:00:00` | HSTS max-age. |
| `Hsts:IncludeSubDomains` | No | `true` | HSTS subdomain coverage. |
| `Hsts:Preload` | No | `false` | Enable only after the domain is ready for preload. |
| `SecurityHeaders:ContentSecurityPolicy` | No | built-in policy | Set to empty to disable CSP. |
| `SecurityHeaders:ReferrerPolicy` | No | `no-referrer` | Referrer policy header. |
| `SecurityHeaders:PermissionsPolicy` | No | `camera=(), microphone=(), geolocation=()` | Permissions policy header. |

See [deployment-hardening.md](../deployment/deployment-hardening.md) for topology-specific
guidance.

## Security-sensitive defaults

- **`AllowedHosts`** is restricted by default. Set it to the public host name(s) so Host-header
  filtering rejects unexpected hosts. Outside Development the host fails fast when it is `*` or
  still the development default (`localhost;127.0.0.1`).
- **Data Protection.** Outside Development the host fails fast when `DataProtection:KeysPath` is
  missing or not writable. The key ring must be persistent and shared by every instance so
  authentication cookies and antiforgery tokens survive restarts.
- **Forwarded headers.** Trust is limited to `ReverseProxy:KnownProxies`/`KnownNetworks`. An empty
  list keeps the framework's loopback-only defaults. Restrict direct access to the application so a
  client cannot spoof `X-Forwarded-Proto`.
- **Cookies.** The production default is `Web:Cookies:SecurePolicy=Always`. Development uses
  `SameAsRequest` only when the section is absent. Keep the authentication, refresh and antiforgery
  cookies `HttpOnly`; see [deployment-hardening.md](../deployment/deployment-hardening.md).
- **Connection strings.** There is no implicit production default. Outside Development the host
  fails fast when `ConnectionStrings:Identity` is missing; `ConnectionStrings:Crm` and
  `ConnectionStrings:Sales` are optional and fall back to the Identity database. The Development
  fallbacks use local credentials only.
- **JWT keys.** Outside Development the host fails fast when signing keys are missing
  ([jwt-key-management.md](jwt-key-management.md)).
- **Demo seeding.** `Demo:SeedIdentity` and `Demo:SeedSampleData` are honoured only when
  `ASPNETCORE_ENVIRONMENT=Development`. Never enable them for a shared or production environment;
  the demo credentials and seeded records are public. See
  [demo readiness](../demo/demo-readiness.md).
- **HSTS / HTTPS redirection** are enabled by default. HSTS is emitted only outside Development.

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
DataProtection__ApplicationName=BSX.Platform
DataProtection__KeysPath=/var/bsx/data-protection-keys
ReverseProxy__KnownNetworks__0=10.20.0.0/24
ReverseProxy__ForwardLimit=1
Web__Cookies__SecurePolicy=Always
```

Omit `ConnectionStrings__Crm` and/or `ConnectionStrings__Sales` to share the Identity database
(each module uses its own `crm`/`sales` schema).

## Configuration validation

`JwtOptionsConfiguration.Apply` (startup) and the host's connection-string check reject invalid or
incomplete configuration before the host begins serving. Failures are logged and the process exits
non-zero.
