# catalog-api 01 — product data

**Status:** Not started

## Goal
Give Catalog.Api its own persistence: the `Product` entity stored in `catalogdb`, with the schema created at startup. Every later catalog spec reads from this table.

The entity, the DbContext and the AppHost wiring are one spec because none of them can be verified alone: the context can't connect without the AppHost reference, and the model is only visible through the context (the table in pgAdmin).

## Context
- Today Catalog.Api only calls `AddServiceDefaults()` and `AddProblemDetails()`, maps `GET /` ("Catalog API is running.") and calls `MapDefaultEndpoints()`. The csproj already references `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.6.0 (added in step 2.1), so no package changes are needed.
- The AppHost already declares `postgres` (with pgAdmin) and the `catalogdb` database on it. `catalog-api` is registered with only `WithHttpHealthCheck("/health")`, with no reference to `catalogdb` and no wait.
- **Connection string: Aspire only.** `WithReference(catalogDb)` injects `ConnectionStrings__catalogdb` into catalog-api, and `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")` reads it by that connection name. There is no appsettings entry, so the API runs only under the AppHost.
- The integration also registers a DbContext health check, so catalog-api's `/health` covers the database connection.
- **Schema creation:** `EnsureCreatedAsync()` runs in `Program.cs`, inline after `builder.Build()` and before `app.Run()`, so the app never serves requests before the table exists. catalog-api/02 adds the seed right after this call.
- The data doesn't persist (no volume, no migrations). Every `aspire run` starts with an empty `catalogdb`, and `EnsureCreated` builds the table again.

## Depends on
Nothing.

## Scope
- [ ] `Models.cs` with `Product { int Id; string Name; string Description; decimal Price }`
- [ ] `CatalogDbContext.cs` with `DbSet<Product> Products` and the price column precision
- [ ] `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")` in `Program.cs`
- [ ] `EnsureCreated()` at startup
- [ ] AppHost: `catalog-api` gets `WithReference(catalogDb).WaitFor(catalogDb)`

## Data model
Table `Products` in `catalogdb` (the EF default name from `DbSet<Product> Products`; SQL must quote it as `"Products"`):

| Property | CLR type | Postgres column | Constraints |
|---|---|---|---|
| `Id` | `int` | `integer`, identity | primary key |
| `Name` | `string` | `character varying(100)` | NOT NULL |
| `Description` | `string` | `character varying(500)` | NOT NULL |
| `Price` | `decimal` | `numeric(18,2)` | NOT NULL |

`Product` stays a plain class. The max lengths, the required flags and the price precision are configured in `CatalogDbContext.OnModelCreating`.

## Tasks
1. `Models.cs`: add the `Product` class with the four properties. `Name` and `Description` default to `string.Empty`.
2. `CatalogDbContext.cs`: add `CatalogDbContext(DbContextOptions<CatalogDbContext>) : DbContext` (primary constructor) with `DbSet<Product> Products`. `OnModelCreating` makes `Name` required with max length 100, makes `Description` required with max length 500, and sets `Price` precision to (18, 2).
3. `Program.cs`: call `builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb")` after `AddServiceDefaults()`. After `builder.Build()`, create a service scope, resolve `CatalogDbContext` and `await db.Database.EnsureCreatedAsync()`, then do the existing pipeline/endpoint mapping and `app.Run()`. Keep the `GET /` placeholder.
4. `AppHost.cs`: chain `.WithReference(catalogDb).WaitFor(catalogDb)` onto the `catalog-api` registration. The other resources don't change.

## Files touched
- `src/EShopClaude.Catalog.Api/Models.cs` (new)
- `src/EShopClaude.Catalog.Api/CatalogDbContext.cs` (new)
- `src/EShopClaude.Catalog.Api/Program.cs` (edit)
- `src/EShopClaude.AppHost/AppHost.cs` (edit: only the `catalog-api` registration)

No changes to the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No seed data (catalog-api/02).
- No endpoints (catalog-api/03, 04).
- No migrations, no data volume.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0 (there are no tests yet; this confirms the suite still builds).
3. From `src/`: `aspire run`. On the dashboard Resources page, `postgres`, the pgAdmin container, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api` and `webfrontend` are all Running and Healthy.
4. On the dashboard: while `catalogdb` is not yet Healthy, `catalog-api` shows the state **Waiting**. It moves to Starting/Running only after `catalogdb` is Healthy.
5. Dashboard → `catalog-api` → resource details → Environment variables: `ConnectionStrings__catalogdb` is present.
6. Dashboard → `catalog-api` → Console logs: an EF Core `CREATE TABLE "Products"` command is logged, and there are no exceptions.
7. Request `GET <catalog-api endpoint>/health` (the URL is on the dashboard) → `200` with body `Healthy`.
8. Open pgAdmin from the dashboard → `catalogdb` → Query Tool, then run
   `SELECT column_name, data_type, character_maximum_length, numeric_precision, numeric_scale, is_nullable FROM information_schema.columns WHERE table_name = 'Products' ORDER BY ordinal_position;`
   → exactly 4 rows:
   | column_name | data_type | character_maximum_length | numeric_precision | numeric_scale | is_nullable |
   |---|---|---|---|---|---|
   | Id | integer | | 32 | 0 | NO |
   | Name | character varying | 100 | | | NO |
   | Description | character varying | 500 | | | NO |
   | Price | numeric | | 18 | 2 | NO |
9. pgAdmin Query Tool on `catalogdb`: `SELECT count(*) FROM "Products";` → `0`.
10. `git diff --stat main` → only this spec and the 4 files listed in Files touched.

## References
- PROJECT.md §3: AppHost resources (`catalog-api` → `catalogdb`, `WaitFor` on each reference) and "Code style per API" (flat files, `decimal` for money).
- PROJECT.md §4.1: Catalog.Api data, startup and integration.
- PROJECT.md §2 "Out of scope": no EF Core migrations, no persistent data volumes.
- PLAN.md 3.1: Catalog.Api.
