# JIMIN-POS

POS & Inventory Management System — C# · Windows Forms · .NET Framework 4.8.1 · SQL Server.

## Solution layout

| Project | Type | Responsibility |
|---|---|---|
| `EnterprisePOS.Domain` | Class library | Entities, `Session`, `Permission`, `Money` |
| `EnterprisePOS.Data` | Class library | SQL Server access (repositories, password hashing) |
| `EnterprisePOS.Services` | Class library | Business rules, validation, permission checks, CSV export |
| `EnterprisePOS.WinForms` | Windows app | Screens (shared `Theme`, `PageBase`, `ThemedDialog`) |

Forms never run SQL; repositories never show message boxes.

## Setup

1. Run `Database/Inoc_database.sql` in SSMS (creates the `Inoc` database, tables and stored procedures).
2. Edit the connection string in `EnterprisePOS.WinForms/App.config` if your server is not `.\SQLEXPRESS`.
3. Set `EnterprisePOS.WinForms` as the startup project and press F5.

Optional settings (same file, `<appSettings>`): `TaxRatePercent`, `BusinessName`, `BusinessAddress`, `BusinessPhone`, `CurrencySymbol`.

## Accounts and roles

Seed accounts come from the SQL script. **Change these passwords before real use.**
The `admin` password is `Admin@123`. Reset the others from *Users & Roles*.

| Role | Can do |
|---|---|
| Cashier | Dashboard, Point of Sale |
| Manager | Everything above + Inventory, Sales Analytics, Sales History (reprint, void) |
| Admin | Everything above + Users & Roles |

Permissions are enforced in the service layer, not only by hiding menu items.

## Screens

- **Login** → sets the signed-in `Session` used for audit columns (inventory log, orders).
- **Dashboard** — today's sales/orders, low-stock count, active products, low-stock list.
- **Point of Sale (F2)** — barcode/SKU scan, search, cart (+ / − / remove / clear), customer, discount, tax, **Checkout (F9)** → payment dialog (Cash/Card/Mobile, change due) → receipt print/preview.
- **Inventory** — search, low-stock filter, add/edit/deactivate product (with image), stock movement history, stock adjustment.
- **Sales Analytics** — date range, revenue/orders/average, three charts, CSV export.
- **Sales History** — find orders, preview/print receipt, void (restores stock).
- **Users & Roles** — create/edit users, change role, reset password, activate/deactivate.

## Notes

- Stock is only changed by `usp_Order_Create`, `usp_Inventory_Adjust` and order voids (all transactional).
- Passwords use the existing `SHA-256(Salt + Password)` scheme so current accounts keep working.
  Plain salted SHA-256 is fast to brute-force; moving to PBKDF2 needs a column/format migration.
- Unexpected errors are written to `%LocalAppData%\JIMIN-POS\logs\error.log`.
- Do not commit production connection strings or backups (see `.gitignore`).
