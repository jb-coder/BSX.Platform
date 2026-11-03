# Password Reset Delivery (D3)

Password reset follows ADR-016: the reset token is random, only its hash is stored, and the plaintext
token is delivered out of band. The complete customer flow is documented in
[password-recovery.md](../identity/password-recovery.md).

## Delivery Posture

The Identity module still owns the `IPasswordResetNotifier` port. The web host supplies an SMTP
adapter through `AddWebPasswordResetDelivery`:

- When the complete SMTP/public-URL configuration is present, reset email is sent.
- When delivery is not configured, `IsDeliveryEnabled == false` and the request endpoint returns
  **`503 Service Unavailable`** for every request (known and unknown accounts alike).
- SMTP failures are logged without recipient or token data and do not create an account-existence
  signal.
- The plaintext reset token is never persisted or logged; it appears only in the emailed link.

## Required Configuration

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

`PublicBaseUrl` must be externally reachable and should be HTTPS. SMTP credentials belong in the
deployment secret store, never in committed files.

## Enumeration and Token Safety

- Unknown and invalid-but-valid-format email addresses return the same accepted response.
- When delivery is disabled, all requests return the same `503`.
- Reset tokens are hashed, single-use and expire after 30 minutes.
- Requesting a new link replaces and invalidates the previous token.
- The request path is rate-limited per normalized email (`Identity:RateLimiting:PasswordReset`).

## Reset Completion

`CompletePasswordResetCommand`:

1. Hashes the presented token and finds the user.
2. Rejects unknown, expired or used tokens.
3. Replaces the password credential.
4. Rotates the security stamp.
5. Revokes every active session.
6. Marks the token used.

The web adapter clears the current authentication and refresh cookies and redirects to
`/login?reset=success`.

## Integrations

The notifier remains replaceable. A future Notifications module can replace the SMTP adapter with a
provider-backed implementation, or route reset delivery through a durable outbox. The request
handler treats delivery failures as non-enumerating and does not expose provider errors to the user.

## Verification

- `PasswordRecoveryWebTests` covers request, notification capture, reset page, success and failure
  UX.
- `PasswordResetEndpointsTests` covers token reuse, expiry, session revocation, security-stamp
  rotation and refresh rejection.
- `PasswordResetEmailFactoryTests` covers reset-link generation, expiry text and configured/
  unavailable delivery.
- `PasswordResetDeliveryTests` verifies the explicit `503` behavior when delivery is disabled.
