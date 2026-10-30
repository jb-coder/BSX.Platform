# Deployment Hardening

This guide covers the supported deployment topologies, reverse-proxy requirements, TLS
termination, cookie security, Data Protection key persistence and the operational checks needed to
run BSX Platform securely behind Nginx, Traefik, Azure App Service, Azure Container Apps or a
Kubernetes ingress.

The production database is PostgreSQL. Identity, CRM and Sales are separate schemas in one database
by default; see [database-migrations.md](../operations/database-migrations.md).

---

## Supported topologies

| Topology | TLS termination | Forwarded headers | Data Protection |
| --- | --- | --- | --- |
| Direct Kestrel (no proxy) | Kestrel certificate | Not required | Persistent local directory |
| Nginx / Traefik | Proxy | Proxy sets `X-Forwarded-Proto/Host/For` | Shared persistent volume |
| Azure App Service | App Service front end | Platform proxy headers | `%HOME%` persistent directory |
| Azure Container Apps | Ingress | Ingress/proxy headers | Shared volume or mounted store |
| Kubernetes ingress | Ingress controller | Ingress sets forwarded headers | Shared PersistentVolume |

Blazor Server uses a WebSocket circuit. Every proxy in front of the application must support
WebSocket upgrades and must not buffer the circuit connection indefinitely.

---

## Reverse proxy requirements

### Required headers

The proxy must send the original request metadata:

| Header | Purpose | Example |
| --- | --- | --- |
| `X-Forwarded-Proto` | Correct scheme for HTTPS redirection, secure cookies and HSTS | `https` |
| `X-Forwarded-Host` | Correct public host for redirects and generated links | `app.example.com` |
| `X-Forwarded-For` | Correct client IP for session metadata, audit and rate-limit keys | `203.0.113.10` |
| `Host` | Host-header filtering (`AllowedHosts`) | `app.example.com` |

Preserve the original `Host` header at the proxy. `AllowedHosts` is evaluated against `Host`, not
`X-Forwarded-Host`.

### Trusted proxies

Forwarded headers are trusted only from the sources configured in `ReverseProxy`:

```json
{
  "ReverseProxy": {
    "Enabled": true,
    "RedirectHttpToHttps": true,
    "ForwardLimit": 1,
    "KnownProxies": [ "10.20.0.10" ],
    "KnownNetworks": [ "10.20.0.0/24" ]
  }
}
```

- If `KnownProxies` and `KnownNetworks` are both empty, the framework's loopback-only defaults apply.
  This is safe for a same-host sidecar and for direct deployments, but a remote proxy will not be
  trusted.
- When either list is configured, the application replaces the framework defaults with exactly the
  configured values. Add every proxy hop that appends headers.
- `ForwardLimit` is the number of proxy hops to consume from the right side of
  `X-Forwarded-For`. Use `1` for a single proxy and `2` only when two trusted proxies are in front of
  the application.
- Never trust forwarded headers from an untrusted network. An attacker who can reach the application
  directly and spoof `X-Forwarded-Proto` could otherwise influence cookie security and redirects.
  Restrict direct access with network policy/security groups when the proxy is the only intended
  ingress.
- For PaaS topologies with dynamic proxy IPs (App Service, Container Apps), configure
  `KnownNetworks` with the platform's private network ranges (for example `10.0.0.0/8`,
  `172.16.0.0/12`) and keep the application unreachable outside that network. Do not use
  `0.0.0.0/0` unless direct access is impossible.

### Middleware order

`UseForwardedHeaders` runs first, before request logging, HTTPS redirection, HSTS, authentication and
authorization. This ensures the scheme, host and client IP are correct for every downstream
component.

### Redirect behavior

`ReverseProxy:RedirectHttpToHttps` defaults to `true`. With correct `X-Forwarded-Proto: https`, the
application sees HTTPS and does not redirect, so the proxy does not create a redirect loop.

If the proxy terminates TLS **and** performs the HTTP-to-HTTPS redirect itself, keep the forwarded
protocol header and leave the application redirect enabled as a safety net. Disable
`RedirectHttpToHttps` only when the proxy is the sole redirect authority and forwarded headers are
correctly configured.

### Platform examples

**Nginx**

```nginx
location / {
    proxy_pass http://bsx-web:8080;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 3600s;
}
```

Set `ReverseProxy:KnownProxies` to the Nginx address (or `KnownNetworks` to its subnet).

**Traefik**

Traefik sets forwarded headers by default and supports WebSocket upgrades. Set
`ReverseProxy:KnownNetworks` to the Traefik pod/network range.

**Azure App Service**

App Service terminates TLS and forwards the original scheme/host. Set `AllowedHosts` to the App
Service hostname and configure `ReverseProxy:KnownNetworks` to the platform's private ranges (or use
network isolation). Ensure the App Service app setting `WEBSITES_PORT` matches the container port.

**Azure Container Apps**

The ingress terminates TLS. Configure `ReverseProxy:KnownNetworks` to the Container Apps
environment subnet and keep the app internal-only (external ingress only on the environment).

**Kubernetes ingress**

```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
```

Configure `ReverseProxy:KnownNetworks` to the ingress controller pod CIDR. Ensure the ingress
preserves the `Host` header and sets `X-Forwarded-Proto`.

---

## TLS termination

- Terminate TLS at the proxy (or at Kestrel for direct deployments). Never expose plain HTTP to the
  public network.
- Keep HSTS enabled outside Development. The default is one year with subdomains, preload disabled.
  Enable preload only after the domain is ready for permanent HTTPS.
- HSTS is emitted only on HTTPS requests. With correct forwarded headers, TLS-terminated proxy
  requests are treated as HTTPS and receive the header.
- The application redirects HTTP to HTTPS by default. Ensure the proxy forwards
  `X-Forwarded-Proto` so this does not loop.
- Keep the proxy's TLS configuration current (TLS 1.2+, strong ciphers, modern certificates).

---

## Cookie requirements

All browser session cookies are `HttpOnly`. The production-safe secure policy is `Always`.

| Cookie | Name | Purpose | SameSite default |
| --- | --- | --- | --- |
| Authentication ticket | `BSX.Web.Auth` | Blazor web session | `Lax` |
| Refresh token | `BSX.Web.Refresh` | Opaque refresh token | `Strict` |
| Antiforgery | `BSX.Web.Antiforgery` | CSRF protection | `Strict` |

Configuration:

```json
{
  "Web": {
    "Cookies": {
      "SecurePolicy": "Always",
      "AuthSameSite": "Lax",
      "RefreshSameSite": "Strict",
      "AntiforgerySameSite": "Strict",
      "Domain": null
    }
  }
}
```

- `SecurePolicy=Always` prevents a downgrade if the proxy forwards the wrong scheme. Development
  defaults to `SameAsRequest` when the section is absent so the local HTTP launch profile works.
- Leave `Domain` empty for host-only cookies. Set it only when the application is intentionally
  shared across subdomains.
- Do not change the cookie names during a rolling deployment unless a forced re-login is acceptable.
- The refresh token is a bearer credential: it is never exposed to JavaScript and must only travel
  over TLS.

---

## Data Protection

The authentication ticket and antiforgery token are protected by ASP.NET Core Data Protection. The
key ring must survive restarts and must be readable by every web instance.

```json
{
  "DataProtection": {
    "ApplicationName": "BSX.Platform",
    "KeysPath": "/var/bsx/data-protection-keys"
  }
}
```

- **Outside Development, `DataProtection:KeysPath` is required.** The host fails fast if it is
  missing so an ephemeral container key ring cannot silently invalidate cookies on restart.
- The directory must be writable by the application process and readable only by the platform.
  Use a mounted persistent volume, a shared filesystem, or the platform's persistent home directory.
- Every instance must use the same `ApplicationName` and the same key directory. A different
  application name or key ring isolates the keys and invalidates other instances' cookies.
- The key ring is the source of truth for authentication tickets and antiforgery tokens; losing it
  forces every user to sign in again. Treat it as persistent state, not disposable cache.
- The readiness check `data-protection` protects and unprotects a payload, so a broken key ring
  fails `/health/ready`.
- Key rotation is automatic. Keep old keys until their lifetime elapses; do not delete the directory
  contents during a rolling deployment.

### Platform storage

| Platform | Recommended `KeysPath` |
| --- | --- |
| Direct / VM | `/var/lib/bsx/data-protection-keys` |
| Docker / Compose | Mounted volume, for example `/var/bsx/keys` |
| Azure App Service | `%HOME%\ASP.NET\DataProtection-Keys` |
| Azure Container Apps | Mounted Azure Files volume, for example `/mnt/bsx-keys` |
| Kubernetes | PersistentVolume mounted at `/var/bsx/keys` |

---

## Security headers

The application emits the following headers on every response:

| Header | Value |
| --- | --- |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `Content-Security-Policy` | Self + configured Google Fonts + Blazor WebSocket |
| `Strict-Transport-Security` | Outside Development, on HTTPS |

The CSP permits the framework's inline import map, the Blazor WebSocket circuit and the configured
Google Fonts stylesheet/font host. Override `SecurityHeaders:ContentSecurityPolicy` to tighten or
disable it. HSTS is configurable under `Hsts` (`MaxAge`, `IncludeSubDomains`, `Preload`).

---

## Health checks

| Path | Purpose | Checks |
| --- | --- | --- |
| `/health/live` | Liveness | `self` process check |
| `/health/ready` | Readiness | Identity database, JWT keys, CRM/Sales databases, optional schema checks, Data Protection |

Route readiness only after the database, signing keys and Data Protection key ring are usable. Use
liveness to restart a stuck process, not to gate traffic.

Enable schema checks where the deployment owns the schema:

```json
{
  "Identity": { "Database": { "VerifySchema": true } },
  "Crm": { "Database": { "VerifySchema": true } },
  "Sales": { "Database": { "VerifySchema": true } }
}
```

---

## Multi-instance considerations

- **Data Protection key ring:** shared and persistent (required).
- **JWT signing keys:** shared via the secret store (required); see
  [jwt-key-management.md](../operations/jwt-key-management.md).
- **Pending workspace selection:** `PendingLoginStore` is in-process for five minutes. A
  multi-instance deployment needs session affinity or a distributed replacement for the selection
  window. Without it, a user may be asked to sign in again if the selection lands on another node.
- **Rate limiting:** the default limiter is per instance. See
  [rate-limiting.md](../operations/rate-limiting.md) for the distributed path.
- **Database:** sessions and refresh tokens are database-backed, so no session affinity is required
  for normal authenticated requests.
- **Migrations:** apply them out of band before rolling out the new version.

---

## Startup validation

Outside Development the host fails fast when:

- `ConnectionStrings:Identity` is missing.
- JWT issuer/audience/lifetime are invalid, or signing keys are missing.
- `DataProtection:KeysPath` is missing or not a writable directory.
- `AllowedHosts` is `*` or still the development default (`localhost;127.0.0.1`).
- A configured trusted proxy IP/CIDR cannot be parsed.

These checks are intentional: a deployment that cannot preserve sessions or authenticate users must
not start behind a proxy and fail later.

---

## Operational checklist

1. Set `AllowedHosts` to the public host name(s); never `*`.
2. Configure `DataProtection:ApplicationName` and a persistent, shared `DataProtection:KeysPath`.
3. Configure `ReverseProxy:KnownProxies`/`KnownNetworks` for the proxy hop(s); set `ForwardLimit`.
4. Ensure the proxy sends `X-Forwarded-Proto`, `X-Forwarded-Host`, `X-Forwarded-For` and preserves
   `Host`.
5. Confirm the proxy supports WebSocket upgrades for the Blazor circuit.
6. Keep HSTS and HTTPS redirection enabled; verify no redirect loop through the proxy.
7. Verify `/health/live` and `/health/ready` through the proxy.
8. Sign in through the proxy and confirm the auth/refresh/antiforgery cookies are `Secure`,
   `HttpOnly` and have the expected `SameSite`.
9. Restart one instance and confirm existing sessions continue to work (Data Protection persistence).
10. If multiple instances are used, confirm a session created on one instance is accepted by another.

See [deployment-checklist.md](../operations/deployment-checklist.md) for the full pre/post deployment
runbook.
