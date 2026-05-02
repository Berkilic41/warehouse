# Warehouse & Inventory Management System

[![CI](https://github.com/Berkilic41/warehouse/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Berkilic41/warehouse/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

Multi-user warehouse management built with ASP.NET Core MVC (.NET 8), SQL Server with **stored procedures**, and ADO.NET (no ORM). Three-layer architecture: `Warehouse.Data` → `Warehouse.Service` → `Warehouse.Web`.

## Why I Built This

Inventory management is a core concern for any business that handles physical goods. I built this system to model the real challenges: **concurrent stock movements, multi-role access control, and aggregate reporting** over large transaction histories. By routing complex queries (dashboard KPIs, stock reports, top-consumed lists) through dedicated stored procedures, and handling stock updates inside atomic transactions with `UPDLOCK`, I practiced the same patterns used in production ERP and logistics backends.

## 🔑 Technical Highlights

- **Atomic stock movements** — Every `StockMovement` (In/Out/Adjustment) updates `MovementItems` and `Products.CurrentStock` in a single transaction; `UPDLOCK` on stock rows prevents race conditions on concurrent out-movements
- **Batch INSERT** — Multi-item movements use a single batched `VALUES` statement instead of N round-trips (reduced from 200 DB calls to 2 for a 100-item movement)
- **Stored-procedure reporting** — `sp_GetDashboardStats`, `sp_GetStockReport`, `sp_GetMovementHistory`, `sp_GetTopConsumed` keep aggregate queries in T-SQL where they perform best
- **Role-based access** — `[Authorize(Roles = "Admin,Staff")]` on all write operations; Viewer role enforced consistently across all controllers
- **47+ unit tests** — MovementService (race conditions, validation), ProductService (SKU uniqueness, category checks), AuthService (xUnit + Moq)
- **Correlation IDs** — Every request gets `X-Correlation-ID` injected into Serilog's `LogContext`
- **Containerized** — Multi-stage Dockerfile + docker-compose with SQL Server 2022 healthcheck

---

## Features

### Roles
| Role | Capabilities |
|---|---|
| **Admin**  | Full access — manage users, categories, products, suppliers, movements |
| **Staff**  | Create/edit products & suppliers, record stock movements |
| **Viewer** | Read-only access to dashboard, products, suppliers, reports |

### Inventory
- **Products**: SKU (unique), name, category, unit, minimum stock threshold, current stock, unit price
- **Categories**: simple hierarchy with product counts
- **Suppliers**: contact info + many-to-many product relationships
- **Stock movements**: three types
  - **In** — incoming stock (purchases) with optional supplier and unit prices
  - **Out** — outgoing stock (sales/transfers), with stock-availability validation
  - **Adjustment** — signed corrections (positive or negative)
- All movements are atomic — one transaction updates `MovementItems` rows + `Products.CurrentStock`

### Dashboard
- Total products / low-stock count / supplier count / 30-day movement count
- Total stock value (`Σ CurrentStock × UnitPrice`)
- Live low-stock alert table
- Top consumed list (last 30 days)

### Reports
- **Stock Levels** — filter by category, low-stock toggle, **CSV export** (client-side)
- **Top Consumed** — date-range query, top-N selector
- **Movement History** — date range + type + product filters

### UX
- Bootstrap 5 + minimal custom CSS
- Razor partial views for reusable bits (`_StockBadge`, `_Layout`)
- **Dynamic form rows** for multi-item movement entries (vanilla JS, see `wwwroot/js/movements.js`)
- **Client-side CSV export** (no server round-trip, see `wwwroot/js/csv-export.js`)

---

## Architecture

```
Warehouse/
├── Database/
│   ├── 001_Schema.sql            ← tables, indexes, constraints
│   ├── 002_StoredProcedures.sql  ← sp_GetStockReport, sp_GetMovementHistory, sp_GetTopConsumed, sp_GetDashboardStats
│   └── 003_SeedData.sql          ← demo users, categories, suppliers, products, sample movements
└── src/
    ├── Warehouse.Data/            ← Data layer (no dependency on Service or Web)
    │   ├── DbConnectionFactory.cs
    │   ├── Entities/              ← POCOs mirroring DB rows
    │   └── Repositories/          ← ADO.NET data access; stored procedures called via CommandType.StoredProcedure
    │       └── Interfaces/
    ├── Warehouse.Service/         ← Service layer (depends only on Data)
    │   ├── Services/              ← business logic (stock validation, role-based access)
    │   ├── DTOs/                  ← request/response records
    │   └── Helpers/               ← PasswordHasher (HMAC-SHA512)
    └── Warehouse.Web/             ← MVC web layer (depends on Service)
        ├── Controllers/           ← Account, Home, Products, Suppliers, Movements, Reports, Admin
        ├── Views/                 ← Razor with partials
        ├── ViewModels/            ← form & list models
        ├── wwwroot/
        │   ├── css/site.css
        │   └── js/                ← movements.js, csv-export.js
        ├── Program.cs
        └── appsettings.json
```

Project references enforce direction: **Web → Service → Data**. The Data layer has no knowledge of MVC, the Service layer has no knowledge of HTTP.

### Stored procedures

Complex queries live in T-SQL where they belong:

| Stored procedure | Purpose |
|---|---|
| `sp_GetStockReport` | Current stock levels with low-stock flag and stock value |
| `sp_GetMovementHistory` | Movement journal filtered by date range, type, product |
| `sp_GetTopConsumed` | Most-consumed products in a date window (top-N) |
| `sp_GetDashboardStats` | One-shot KPI fetch (5 metrics) |

CRUD on individual rows uses inline parameterized SQL.

---

## Prerequisites

- **.NET SDK 8.0+** — [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **SQL Server**: LocalDB (included with Visual Studio) or SQL Server Express 2022+
- **sqlcmd** — SQL Server command-line tool

Verify:
```bash
dotnet --version   # 8.0.x or later
sqlcmd -?          # SQL Server tools installed?
```

If `sqlcmd` is not found, add to PATH: `C:\Program Files\Microsoft SQL Server\150\Tools\Binn` (adjust version number).

---

## Getting Started

### 1. Clone

```bash
git clone https://github.com/your-username/Warehouse.git
cd Warehouse
```

### 2. Create the database

**LocalDB:**
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -i Database\001_Schema.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database\002_StoredProcedures.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database\003_SeedData.sql
```

**SQL Server Express:**
```bash
sqlcmd -S ".\SQLEXPRESS" -i Database\001_Schema.sql
sqlcmd -S ".\SQLEXPRESS" -i Database\002_StoredProcedures.sql
sqlcmd -S ".\SQLEXPRESS" -i Database\003_SeedData.sql
```

Verify:
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases WHERE name='WarehouseDb';"
```

### 3. Update connection string (if needed)

`src/Warehouse.Web/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WarehouseDb;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

For production, set via environment variable: `ConnectionStrings__DefaultConnection`

### 4. Run

```bash
cd src/Warehouse.Web
dotnet restore
dotnet run
```

HTTP: `http://localhost:5028`

### Demo accounts

All seed users have password **`password123`**:

| Email | Role | Capabilities |
|---|---|---|
| `admin@warehouse.test`  | Admin  | Full access — users, categories, products, suppliers, movements |
| `staff@warehouse.test`  | Staff  | Create/edit products & suppliers, record stock movements |
| `viewer@warehouse.test` | Viewer | **Read-only** — view dashboard, products, suppliers, reports; export CSV |

### Viewer Role (Read-Only)

The Viewer role is enforced via `[Authorize(Roles = "Admin,Staff")]` on all write operations. Viewers **can**:
- View dashboard KPIs and low-stock alerts
- Browse products, suppliers, movement history
- View stock reports and top-consumed lists, export CSV

Viewers **cannot** create, edit, delete, or manage anything.

---

## Environment Variables

Override connection string via environment variable (CI/CD, Docker):

```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=WarehouseDb;..."
dotnet run
```

---

## CI/CD

GitHub Actions workflow at `.github/workflows/dotnet.yml` runs on every push:
- **Build** → **Test** → **Vulnerability Scan** (PRs) → **Publish artifact** (main only)

Enable by pushing `.github/` to your repository.

---

## Troubleshooting

**sqlcmd not found:** Add `C:\Program Files\Microsoft SQL Server\150\Tools\Binn` to PATH and restart terminal.

**Port conflict:**
```bash
netstat -ano | findstr :5028
taskkill /PID <PID> /F
```

**DB connection fails:** Verify server name in `appsettings.json` and that SQL Server service is running.

**Demo login fails:** Verify seed data:
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d WarehouseDb -Q "SELECT COUNT(*) FROM Users;"
# Should return 3. If not, re-run Database\003_SeedData.sql
```

---

## Routes

| Path | Auth | Description |
|---|---|---|
| `/` | All authenticated | Dashboard with KPIs and low-stock alerts |
| `/Products` | All authenticated | Product list with search & filter |
| `/Products/Create` | Admin / Staff | New product |
| `/Products/Edit/{id}` | Admin / Staff | Edit product |
| `/Suppliers` | All authenticated | Supplier list |
| `/Suppliers/Create` | Admin / Staff | New supplier with product links |
| `/Movements` | All authenticated | Movement history with filters |
| `/Movements/Create?type=In` | Admin / Staff | Record stock movement (multi-row form) |
| `/Movements/Details/{id}` | All authenticated | Movement detail view |
| `/Reports/Stock` | All authenticated | Stock report with CSV export |
| `/Reports/TopConsumed` | All authenticated | Top consumed products |
| `/Admin/Users` | Admin | User management |
| `/Admin/Categories` | Admin | Category management |

---

## Screenshots

> _Add your screenshots here once the app is running:_
>
> ![Dashboard](docs/screenshots/dashboard.png)
> ![Stock Report](docs/screenshots/stock-report.png)
> ![New Movement](docs/screenshots/new-movement.png)
> ![Suppliers](docs/screenshots/suppliers.png)

---

## Security

- Passwords hashed with **HMAC-SHA512** + per-user 64-byte salt
- Cookie auth — `HttpOnly`, `Secure`, `SameSite=Strict`, 8-hour session
- Anti-forgery tokens on every state-changing form
- All SQL parameterized — no string concatenation
- Stock-out movements validate availability before commit (with `UPDLOCK` to prevent race conditions)
- Movements + stock updates wrapped in a single SQL transaction
- Security headers: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Content-Security-Policy`, `Referrer-Policy`
- Rate limiting on login endpoint (10 requests/min per IP)

---

## License

MIT — see [LICENSE](LICENSE)
