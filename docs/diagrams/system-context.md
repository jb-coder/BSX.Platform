# System Context Diagram

```mermaid
flowchart TD

User[User]

User --> Web[BSX Web]

Web --> Identity
Web --> CRM
Web --> Sales
Web --> Inventory
Web --> Purchasing
Web --> Accounting

Identity --> PostgreSQL[(PostgreSQL)]
CRM --> PostgreSQL
Sales --> PostgreSQL
Inventory --> PostgreSQL
Purchasing --> PostgreSQL
Accounting --> PostgreSQL
```