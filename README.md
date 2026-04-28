# Warehouse & Inventory Management System

Multi-user warehouse management built with ASP.NET Core MVC (.NET 8), SQL Server with **stored procedures**, and ADO.NET (no ORM). Three-layer architecture: `Warehouse.Data` → `Warehouse.Service` → `Warehouse.Web`.

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

## Setup

### 1. Database (one-time)

```bash
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/001_Schema.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/002_StoredProcedures.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/003_SeedData.sql
```

### 2. Run

```bash
cd src/Warehouse.Web
dotnet run
```

App opens on `http://localhost:5028` (or whichever port `launchSettings.json` picks).

### 3. Demo accounts

All seed users have password **`password123`**:

| Email | Role | Notes |
|---|---|---|
| `admin@warehouse.test`  | Admin  | Full access |
| `staff@warehouse.test`  | Staff  | Can record movements, manage products/suppliers |
| `viewer@warehouse.test` | Viewer | Read-only |

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
- Cookie auth, HTTP-only, 14-day sliding expiration
- Anti-forgery tokens on every state-changing form
- All SQL parameterized — no string concatenation
- Stock-out movements validate availability before commit
- Movements + stock updates wrapped in a single SQL transaction
