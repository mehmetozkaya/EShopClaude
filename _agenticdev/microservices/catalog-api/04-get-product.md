# catalog-api 04 — get product

**Status:** Done

## Goal
Expose a single product by id. Basket.Api calls this endpoint when an item is added, to check the product exists and to snapshot its name and price.

## Context
- catalog-api/01–03 are merged: `Product` (in `Models.cs`), `CatalogDbContext` with `DbSet<Product> Products`, registered with `builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`. Startup runs `EnsureCreatedAsync()` and `CatalogDbContext.SeedAsync(db)`, so the 8 seeded products (Ids 1–8) exist. `GET /products` is already mapped and `Catalog.Api.http` exists.
- The `Product` entity is returned as is; its JSON shape `{ id, name, description, price }` is the API contract. No separate DTO.
- The query is read-only, so it uses `AsNoTracking()`. The Aspire Npgsql EF integration already emits EF/Npgsql tracing, so no extra OTel setup is needed for the trace check.
- The route constraint `{id:int}` keeps the parameter typed; a non-integer id doesn't match the route.

## Depends on
catalog-api/02-product-seed

## Scope
- [ ] `GET /products/{id}` returns `200 Product`, or `404` when it doesn't exist
- [ ] Add the request to `Catalog.Api.http`

## Tasks
1. `Program.cs`: after `GET /products`, map
   ```csharp
   app.MapGet("/products/{id:int}", async (int id, CatalogDbContext db) =>
       await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id) is { } product
           ? Results.Ok(product)
           : Results.NotFound());
   ```
2. `Catalog.Api.http`: append a `GET {{Catalog.Api_HostAddress}}/products/1` request and a `GET {{Catalog.Api_HostAddress}}/products/9999` (404) request.

## Files touched
- `src/EShopClaude.Catalog.Api/Program.cs` (edit)
- `src/EShopClaude.Catalog.Api/Catalog.Api.http` (edit)

No changes to `Models.cs`, `CatalogDbContext.cs`, the AppHost, the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No product detail page or extra product fields.
- No write endpoints.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard every resource is Running and Healthy.
4. `GET http://localhost:5101/products/1` → `200`, `{ "id": 1, "name": "Wireless Mouse", ..., "price": 24.99 }`.
5. `GET http://localhost:5101/products/9999` → `404`.
6. Dashboard → Traces → the `catalog-api` `GET /products/{id}` trace has an EF/Npgsql child span running `SELECT ... FROM "Products" ... WHERE p."Id" = @id ...`.
7. `git diff --stat main` → only this spec, the README status table and the 2 files listed in Files touched.

## References
- PROJECT.md §4.1: Catalog.Api endpoints (`GET /products/{id}` → `200 Product` or `404`).
- PROJECT.md §4.2: Basket.Api `POST /baskets/{buyerId}/items` calls this endpoint (404 → 404).
- PROJECT.md §3 "Code style per API": endpoints in `Program.cs`.
- PLAN.md 3.1: Catalog.Api.
