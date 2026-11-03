# Password Recovery

Status: **Pilot-ready when SMTP delivery is configured.**

Password recovery uses the existing Identity reset-token model and the existing
`IPasswordResetNotifier` port. The web host supplies an SMTP notifier; no parallel notification
framework was introduced.

---

## Customer Flow

```text
/forgot-password
  -> POST /auth/password-reset/request
  -> RequestPasswordResetCommand
  -> token hash persisted and plaintext token delivered by email
  -> user opens /reset-password?token=...
  -> POST /auth/password-reset/complete
  -> CompletePasswordResetCommand
  -> password changed, token used, security stamp rotated, all sessions revoked
  -> /login?reset=success
  -> user signs in with the new password
```

The request page always returns the same accepted response for a valid email, whether or not the
account exists. Invalid email format, unavailable delivery and rate limiting are non-account-specific
errors.

---

## Token Lifecycle

| Property | Behavior |
| --- | --- |
| Generation | 256-bit cryptographically secure opaque token. |
| Storage | Only a SHA-256 hash is persisted in `identity.password_reset_tokens`. |
| Lifetime | 30 minutes. |
| Re-request | Replaces the previous token and invalidates it. |
| Single use | Marked used on successful completion; replay is rejected. |
| Expiry | Expired tokens are rejected. |
| Logging | The plaintext token is never logged or persisted outside the email delivery call. |
| Reset completion | Rotates the user security stamp and revokes every active session. |

The reset token is not a login credential. It only authorizes the completion command once and
cannot be exchanged for a session directly.

---

## Email Delivery

The host-level `SmtpPasswordResetNotifier` implements the existing
`BSX.Modules.Identity.Application.Abstractions.IPasswordResetNotifier` port.

- It builds an absolute link from `PublicBaseUrl` + `ResetPath`.
- It sends an HTML email with a plain-text alternative.
- The plaintext token exists only in the reset link in the email.
- The notifier does **not** log the recipient or the token.
- SMTP failures are logged without recipient/token data and do not reveal whether an account exists.
- If delivery is not configured, `IsDeliveryEnabled` is false and the request endpoint returns
  `503` for every caller.

The current adapter is a synchronous SMTP send from the request path. There is no outbox retry for
password-reset email yet; a user can request another link if delivery fails.

### Configuration

```json
{
  "Identity": {
    "PasswordReset": {
      "Delivery": {
        "Enabled": true,
        "PublicBaseUrl": "https://app.example.com",
        "ResetPath": "/reset-password",
        "Host": "smtp.example.com",
        "Port": 587,
        "UseStartTls": true,
        "UserName": "smtp-user",
        "Password": "use-secret-store",
        "FromAddress": "no-reply@example.com",
        "FromName": "BSX Platform"
      }
    }
  }
}
```

Environment-variable form:

```text
Identity__PasswordReset__Delivery__Enabled=true
Identity__PasswordReset__Delivery__PublicBaseUrl=https://app.example.com
Identity__PasswordReset__Delivery__Host=smtp.example.com
Identity__PasswordReset__Delivery__Port=587
Identity__PasswordReset__Delivery__UseStartTls=true
Identity__PasswordReset__Delivery__UserName=smtp-user
Identity__PasswordReset__Delivery__Password=***
Identity__PasswordReset__Delivery__FromAddress=no-reply@example.com
Identity__PasswordReset__Delivery__FromName=BSX Platform
```

Do not commit SMTP credentials or the production public URL. Supply both through the deployment
secret/configuration path.

`PublicBaseUrl` must be the externally reachable HTTPS origin. The reset link is not derived from
the request host because the request may arrive through a proxy or API client.

---

## Session and Security Behavior

On successful reset:

1. The token is marked used.
2. The password credential is replaced.
3. The user security stamp is rotated.
4. Every active session for the user is revoked.
5. Refresh tokens referencing those sessions are no longer usable.
6. The current browser cookie and refresh cookie are cleared by the web adapter.

This is the safest behavior consistent with ADR-017: a password reset is a credential change, so
all existing authentication artifacts must be invalidated. The user must sign in again.

Authorization revalidation continues to fail closed if a store read fails. Role and permission
changes do not revoke sessions; password changes do.

---

## Web UX

| Route | Purpose |
| --- | --- |
| `/forgot-password` | Request a reset link. |
| `/reset-password?token=...` | Set a new password. |
| `/auth/password-reset/request` | Server adapter over `RequestPasswordResetCommand`. |
| `/auth/password-reset/complete` | Server adapter over `CompletePasswordResetCommand`. |
| `/login?reset=success` | Confirmation after a successful reset. |

The pages are static SSR because they need the initial request's antiforgery cookie and do not
require an interactive circuit.

### States

- **Sent:** “If an account exists, a reset link has been sent.” No account-existence signal.
- **Invalid/expired token:** explain that the link is no longer usable and offer a new link.
- **Password mismatch:** retain the token and show a field-level guidance alert.
- **Password rule failure:** explain the minimum length.
- **Success:** sign in again with the new password.
- **Delivery unavailable:** explain that recovery is unavailable and direct the user to their
  workspace administrator.

---

## Security Checklist

- [x] Token hashed at rest.
- [x] Token single-use.
- [x] Token expiry enforced.
- [x] Previous token invalidated by a new request.
- [x] No user-enumeration response from request.
- [x] No token in request responses or application logs.
- [x] Reset completion revokes all sessions.
- [x] Security stamp rotated.
- [x] Refresh tokens for revoked sessions fail closed.
- [x] Antiforgery on both web forms.
- [x] Request rate limiting per normalized email.
- [x] SMTP credentials supplied by secret/configuration, not source.

---

## Operational Considerations

### Before a pilot

1. Configure a real SMTP relay and verified sender.
2. Set `Identity__PasswordReset__Delivery__Enabled=true`.
3. Set the externally reachable `PublicBaseUrl`.
4. Verify the reset mail reaches an external mailbox, not only an internal test inbox.
5. Confirm the reset link points to the public host and is not rewritten incorrectly by the proxy.
6. Run the full password-recovery smoke test below.
7. Keep SMTP credentials and the public URL in the deployment secret store.

### Smoke test

1. Request recovery for a known pilot account.
2. Confirm the email arrives.
3. Open the link on a different browser/device.
4. Set a new password.
5. Confirm the old password fails and the new password succeeds.
6. Confirm an existing session is signed out and its refresh token is rejected.
7. Confirm a second use of the same link is rejected.
8. Confirm an expired link shows the expired state.
9. Confirm a request for an unknown account returns the same accepted response.

### Troubleshooting

| Symptom | Check |
| --- | --- |
| Request returns `503` | `Enabled`, `Host`, `FromAddress`, `PublicBaseUrl` are incomplete. |
| Email not received | SMTP relay, sender verification, spam filtering, and delivery logs. |
| Link points to localhost | `PublicBaseUrl` is not the public origin. |
| Link opens but reset fails | Token expired/used, or the token was replaced by a newer request. |
| User still has a session | Confirm the reset completed and the session store is reachable. |

### Current limitations

- Delivery is direct SMTP; there is no durable outbox retry for reset email yet.
- There is no administrative “force reset” UI. A pilot operator should use the same self-service
  flow or the documented Identity application command path.
- Reset requests are rate-limited per email; a per-IP limiter remains an operational hardening item.
- The reset page token appears in the email URL and browser history, as with any emailed reset link.
  The page uses `no-referrer` and does not render the token in page content outside the hidden form.
