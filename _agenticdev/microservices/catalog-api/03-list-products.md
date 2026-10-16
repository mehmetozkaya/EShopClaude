# catalog-api 03 — list products

**Status:** Done

## Goal
Expose the catalog to callers with `GET /products`. The storefront's Products page (use case 1) lists products through this endpoint.

## Context
- catalog-api/01 and /02 are merged: `Product` (in `Models.cs`), `CatalogDbContext` with `DbSet<Product> Products`, registered with `builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`. `Program.cs` runs `EnsureCreatedAsync()` and `CatalogDbContext.SeedAsync(db)` in a startup scope, so the 8 seeded products (Ids 1–8) exist before the app serves requests.
- Catalog.Api currently maps only `GET /` and the default endpoints.
- The `Product` entity is returned as is; its JSON shape `{ id, name, description, price }` is the API contract. No separate DTO.
- The query is read-only, so it uses `AsNoTracking()`. The Aspire Npgsql EF integration already emits EF/Npgsql tracing, so no extra OTel setup is needed for the trace check.

## Depends on
catalog-api/02-product-seed

## Scope
- [ ] `GET /products` returns `200 Product[]` ordered by Id
- [ ] `Catalog.Api.http` with the list request

## Tasks
1. `Program.cs`: add `using Microsoft.EntityFrameworkCore;` and map
   `app.MapGet("/products", async (CatalogDbContext db) => await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync());`
   after the existing `GET /`.
2. Create `Catalog.Api.http` with `@Catalog.Api_HostAddress = http://localhost:5101` and a `GET {{Catalog.Api_HostAddress}}/products` request.

## Files touched
- `src/EShopClaude.Catalog.Api/Program.cs` (edit)
- `src/EShopClaude.Catalog.Api/Catalog.Api.http` (new)

No changes to `Models.cs`, `CatalogDbContext.cs`, the AppHost, the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No `GET /products/{id}` (catalog-api/04).
- No paging, filtering or search.
- No write endpoints.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard every resource is Running and Healthy.
4. `GET http://localhost:5101/products` (the `.http` request or `curl`) → `200`, a JSON array of the 8 seeded products from catalog-api/02, Ids 1–8 in ascending order.
5. Dashboard → Traces → the `catalog-api` `GET /products` trace has an EF/Npgsql child span running `SELECT ... FROM "Products" ... ORDER BY p."Id"`.
6. `git diff --stat main` → only this spec, the README status table and the 2 files listed in Files touched.

## References
- PROJECT.md §4.1: Catalog.Api endpoints (`GET /products` → `200 Product[]` ordered by Id).
- PROJECT.md §2 "Out of scope": no paging, no admin/CRUD for products.
- PROJECT.md §3 "Code style per API": endpoints in `Program.cs`.
- PLAN.md 3.1: Catalog.Api.
