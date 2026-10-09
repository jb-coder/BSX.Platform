# Authorization

Permission-based authorization evaluated in the pipeline (ADR-012).

## Components

```mermaid
flowchart LR

    subgraph Host
        PRIN["ClaimsPrincipal<br/>(post-authentication)"]
    end

    subgraph BuildingBlocks["BuildingBlocks.Authorization"]
        CU[ICurrentUser]
        PC[IPermissionChecker]
        RQ[RequirePermissionAttribute]
        AB[AuthorizationBehavior]
    end

    subgraph Identity["Identity (future)"]
        STORE["Roles + Permissions"]
        CHECKER[PermissionCheckerImpl]
    end

    subgraph Module
        HND["Command / Query Handler"]
    end

    PRIN --> CU
    CU --> AB
    RQ --> AB
    AB --> PC
    PC --> CHECKER
    CHECKER --> STORE
    AB -->|allowed| HND
```

## Pipeline Decision

```mermaid
flowchart TD
    REQ[Request] --> HAS{"RequirePermission<br/>declared?"}
    HAS -->|no| ALLOW[Continue to handler]
    HAS -->|yes| AUTH{Authenticated?}
    AUTH -->|no| E401["Result<br/>(Authentication)"]
    AUTH -->|yes| PERM{Has permission?}
    PERM -->|no| E403["Result<br/>(Permission)"]
    PERM -->|yes| ALLOW
```

## Defaults Before Identity

```mermaid
flowchart LR
    NOTREG[No Identity registered] --> CU["ICurrentUser = anonymous<br/>(always registered)"]
    NOTREG --> PC["IPermissionChecker = fail-closed"]
    PC -->|"request has requirement"| DENY["Denied (Permission)"]
    PC -->|"request has no requirement"| PASS[Unaffected]
```

- The gate is the permission (`resource.action`), not the role.
- Resource-level rules (ownership, tenant, entity state) are evaluated by the handler using
  `ICurrentUser`.
- Authentication (issuance/validation) is owned by Identity; the reserved permission claim type
  is `permission`.
