# catalog-api 02 — product seed

**Status:** In progress

## Goal
Fill the read-only catalog with demo products, so the storefront and the basket have something to list and add. The catalog has no admin, so the seed is the only source of products.

## Context
- catalog-api/01 is merged: `Product` (in `Models.cs`), `CatalogDbContext` with `DbSet<Product> Products`, and `Program.cs` calls `await db.Database.EnsureCreatedAsync()` inside a startup service scope, right after `builder.Build()`. The `Products` table is empty.
- CLAUDE.md puts the seed in `CatalogDbContext.cs` ("XxxDbContext.cs (with a seed)"). It is a runtime seed (`AnyAsync` check, then `AddRange` + `SaveChangesAsync`), not EF `HasData`, so the "only when the table is empty" rule is explicit in code.
- The seed runs in the same startup scope, right after `EnsureCreatedAsync()`, so the app never serves requests before the products exist.
- The data doesn't persist (no volume). Every AppHost start begins with an empty `catalogdb` and seeds again. Restarting only `catalog-api` while `postgres` keeps running finds the table non-empty and skips the seed, so there are never duplicates.
- `Id`s are not set in code; the identity column assigns 1..8 in insertion order on a fresh database.

## Depends on
catalog-api/01-product-data

## Scope
- [ ] Code seed of about 8 products, run at startup after `EnsureCreated()`
- [ ] Seed only when the `Products` table is empty
- [ ] Prices as `decimal`

## Seed data
| # | Name | Description | Price |
|---|---|---|---|
| 1 | Wireless Mouse | Ergonomic 2.4 GHz wireless mouse with silent clicks. | 24.99 |
| 2 | Mechanical Keyboard | Tenkeyless keyboard with tactile brown switches. | 89.00 |
| 3 | USB-C Hub | 7-in-1 hub with HDMI, USB-A, SD and 100 W pass-through. | 39.50 |
| 4 | 27" Monitor | 27-inch QHD IPS monitor, 75 Hz, adjustable stand. | 249.00 |
| 5 | Noise-Cancelling Headphones | Over-ear Bluetooth headphones with 30-hour battery. | 179.99 |
| 6 | Laptop Stand | Aluminium stand that raises the screen to eye level. | 34.95 |
| 7 | Webcam HD | 1080p webcam with dual microphones and privacy cover. | 59.00 |
| 8 | Desk Mat | Large 90 × 40 cm water-resistant desk mat. | 19.90 |

## Tasks
1. `CatalogDbContext.cs`: add `public static async Task SeedAsync(CatalogDbContext db)`. If `await db.Products.AnyAsync()` is true, return. Otherwise `AddRange` the 8 products from the Seed data table (prices as `decimal` literals, no `Id`) and `await db.SaveChangesAsync()`.
2. `Program.cs`: in the existing startup scope, call `await CatalogDbContext.SeedAsync(db);` right after `EnsureCreatedAsync()`, and update the comment above the scope.

## Files touched
- `src/EShopClaude.Catalog.Api/CatalogDbContext.cs` (edit)
- `src/EShopClaude.Catalog.Api/Program.cs` (edit)

No changes to `Models.cs`, the AppHost, the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No endpoints (catalog-api/03, 04).
- No categories, images or stock.
- No data volume; data resets on AppHost restart by design.
- No migrations, no EF `HasData` model seeding.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard every resource is Running and Healthy.
4. Dashboard → `catalog-api` → Console logs: an EF Core `INSERT INTO "Products"` command is logged, and there are no exceptions.
5. pgAdmin Query Tool on `catalogdb`: `SELECT "Id", "Name", "Description", "Price" FROM "Products" ORDER BY "Id";` → exactly the 8 rows of the Seed data table (Ids 1–8).
6. Dashboard → `catalog-api` → Restart (postgres keeps running). `SELECT count(*) FROM "Products";` → `8` (seed skipped, no duplicates).
7. Stop and start the AppHost again. `SELECT count(*) FROM "Products";` → `8` (fresh database, seeded again).
8. `git diff --stat main` → only this spec and the 2 files listed in Files touched.

## References
- PROJECT.md §4.1: Catalog.Api startup (`EnsureCreated()`, then seed about 8 products if the table is empty).
- PROJECT.md §2 "Out of scope": no admin/CRUD for products, no migrations, no persistent data volumes, no categories/images/stock.
- PROJECT.md §3 "Code style per API": `XxxDbContext.cs` (with a seed), `decimal` for money.
- PROJECT.md §7: pgAdmin shows the seeded `catalogdb` products.
- PLAN.md 3.1: Catalog.Api.
