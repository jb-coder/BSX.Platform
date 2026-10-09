# Identity Hardening - Diagrams

Visual reference for ADR-013 through ADR-018.

---

## 1. Multi-Tenant Identity Model (ADR-013)

```mermaid
flowchart TD
    USER["User<br/>(global identity + credentials)"]
    MEMBER["TenantMembership"]
    ROLE["Role<br/>(tenant-scoped)"]
    SYSROLE["Role<br/>(TenantId = null, system)"]
    PERM["Permission<br/>(value object)"]
    T1["Tenant A"]
    T2["Tenant B"]

    USER -->|"membership"| MEMBER
    MEMBER --> T1
    MEMBER --> T2
    MEMBER -->|"roles in Tenant A"| ROLE
    MEMBER -->|"roles in Tenant B"| ROLE
    ROLE --> PERM
    MEMBER -->|"platform roles"| SYSROLE
    SYSROLE --> PERM
```

---

## 2. Login and Tenant Selection (ADR-013)

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant I as Identity
    participant DB as identity schema
    C->>I: POST /auth/login (email, password)
    I->>DB: resolve global user, verify credential
    alt no active membership
        I-->>C: 200 tokens (no tenant) + profile
    else one membership
        I->>I: select tenant
        I-->>C: 200 tokens scoped to tenant
    else multiple memberships
        I-->>C: 200 tokens (tenant selection required)
        C->>I: POST /tenants/{id}/switch
        I-->>C: 200 tokens scoped to chosen tenant
    end
```

---

## 3. Tenant Switch (ADR-013)

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant I as Identity
    C->>I: POST /identity/tenants/{tenantId}/switch
    I->>I: verify active membership
    alt member
        I-->>C: new access token (tenant + permissions)
    else not a member
        I-->>C: 403 Permission
    end
    Note over C,I: No re-authentication; refresh chain unchanged
```

---

## 4. Credential Model (ADR-014)

```mermaid
flowchart TD
    PRINCIPAL["Principal (abstract)"]
    USER["User (human)"]
    SVC["ServiceAccount (machine)"]
    PWD["PasswordCredential"]
    TOTP["TotpCredential (MFA)"]
    REC["RecoveryCodeCredential"]
    WEBA["WebAuthnCredential (passkey)"]
    EXT["ExternalProviderCredential<br/>(Microsoft, Google)"]
    KEY["ApiKey (separate aggregate)"]

    PRINCIPAL --> USER
    PRINCIPAL --> SVC
    USER --> PWD
    USER --> TOTP
    USER --> REC
    USER --> WEBA
    USER --> EXT
    SVC --> KEY
```

---

## 5. Authentication Factor Flow (ADR-014)

```mermaid
flowchart TD
    START["Authenticate"] --> PRIMARY{"Primary factor"}
    PRIMARY -->|password| PW["Verify PasswordCredential"]
    PRIMARY -->|passkey| WK["Verify WebAuthnCredential"]
    PRIMARY -->|external| EXT["Verify ExternalProviderCredential"]
    PW --> MFA{"MFA required or enrolled?"}
    WK --> MFA
    EXT --> MFA
    MFA -->|no| TOKENS["Issue tokens"]
    MFA -->|yes| CHALLENGE["Challenge TOTP / passkey"]
    CHALLENGE -->|verified| TOKENS
    CHALLENGE -->|recovery| REC["Consume RecoveryCode"]
    REC --> TOKENS
```

---

## 6. Contracts Dependency (ADR-015)

### Before (contradiction)

```mermaid
flowchart LR
    MC["Module.Contracts"] --> SK["BSX.SharedKernel"]
    MC -. "needs IIntegrationEvent" .-> BB["BSX.BuildingBlocks"]
```

### After

```mermaid
flowchart TD
    SK["BSX.SharedKernel"]
    CON["BSX.Contracts<br/>(IIntegrationEvent, shared ids)"]
    BB["BSX.BuildingBlocks"]
    MC["Module.Contracts"]
    MA["Module.Application"]

    SK --> CON
    CON --> BB
    CON --> MC
    BB --> MA
    MC --> MA
```

---

## 7. Password Reset Security (ADR-016)

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant I as Identity
    participant DB as identity schema
    participant OB as Outbox (encrypted)
    participant N as Notifications

    U->>I: POST /auth/password/forgot
    I->>I: generate token, hash it
    I->>DB: store hash, invalidate prior token
    I->>OB: PasswordResetRequested (sensitive, encrypted)
    I-->>U: 202 Accepted (always)
    OB->>N: publish (decrypt just-in-time)
    N->>U: email with reset link
    U->>I: POST /auth/password/reset (token, newPassword)
    I->>DB: verify hash, mark used (atomic), invalidate sessions
    I-->>U: 204
```

At rest: Identity holds only the token **hash**; the Outbox holds only **ciphertext**.

---

## 8. Token Invalidation and Session Lifecycle (ADR-017)

```mermaid
flowchart TD
    LOGIN["Login"] --> SESSION["Session + refresh chain"]
    SESSION --> ACCESS["Access token (15 min, stateless)"]
    ACCESS -->|"expires"| REFRESH{"Refresh"}
    REFRESH -->|"valid"| ROTATE["Rotate (grace 30s)"]
    ROTATE --> ACCESS
    REFRESH -->|"reused"| REVOKE["Revoke chain"]
    SECURITY["Password / status change"] --> STAMP["Rotate security stamp"]
    STAMP --> REVOKE
    REVOKE --> REAUTH["Re-authenticate"]
    PERM["Role / permission change"] -->|"no revoke"| TTL["Propagate within TTL"]
```

---

## 9. Role Lifecycle and System Role Sync (ADR-018)

```mermaid
flowchart TD
    CATALOG["Permission catalog"] --> SYNC["SynchronizePermissions"]
    SYNC --> ADMIN["Administrator = all permissions"]
    SYNC --> AUDIT["Auditor = all read permissions"]
    SYNC --> ROLES["Tenant roles"]
    DELETE["Delete role"] --> CHECK{"Assigned?"}
    CHECK -->|yes| BLOCK["Conflict<br/>(reassign first)"]
    CHECK -->|no| SOFT["Soft delete"]
    SYSTEM["System role"] -->|"cannot delete/rename"| LOCKED["Immutable"]
```

```mermaid
flowchart LR
    CHANGE["Role/permission change"] --> EVENT["Integration events (Outbox)"]
    EVENT --> CACHE1["RolePermissionsChanged"]
    EVENT --> CACHE2["UserRolesChanged"]
    CACHE1 --> INVALIDATE["Consumer cache invalidation"]
    CACHE2 --> INVALIDATE
    CHANGE -->|"token TTL <= 15 min"| TOKENS["Token permission refresh"]
```
