# ordering-api 01 — order data

**Status:** Not started

## Goal
Give Ordering.Api its own persistence: orders and their lines in `orderingdb`, with the schema created at startup. Lines copy the product name and price, so an order never depends on Catalog. Every later ordering spec (create, get, list) reads from or writes to these tables.

The entities, the DbContext and the AppHost wiring are one spec because none of them can be verified alone: the context can't connect without the AppHost reference, and the model is only visible through the context (the tables in pgAdmin).

## Context
- Today Ordering.Api only calls `AddServiceDefaults()` and `AddProblemDetails()`, maps `GET /` ("Ordering API is running.") and calls `MapDefaultEndpoints()`. The csproj already references `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.6.0 (added in step 2.1), so no package changes are needed.
- The AppHost already declares `postgres` (with pgAdmin) and the `orderingdb` database on it. `ordering-api` is registered with only `WithHttpHealthCheck("/health")`, with no reference to `orderingdb` and no wait.
- **Connection string: Aspire only.** `WithReference(orderingDb)` injects `ConnectionStrings__orderingdb` into ordering-api, and `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")` reads it by that connection name. There is no appsettings entry, so the API runs only under the AppHost.
- The integration also registers a DbContext health check, so ordering-api's `/health` covers the database connection.
- **Schema creation:** `EnsureCreatedAsync()` runs in `Program.cs`, inline after `builder.Build()` and before `app.Run()`, so the app never serves requests before the tables exist. There is no seed: orders only come from checkout.
- **Relationship:** a plain one-to-many (`HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId)`, cascade delete), not an EF owned collection. `OrderLine` has no navigation back to `Order`, so serializing an order in ordering-api/03 has no cycle.
- The data doesn't persist (no volume, no migrations). Every `aspire run` starts with an empty `orderingdb`, and `EnsureCreated` builds the tables again.
- Follows the same pattern as catalog-api/01 (`CatalogDbContext`, startup scope in `Program.cs`).

## Depends on
Nothing.

## Scope
- [ ] `Models.cs` with `Order { int Id; string BuyerId; DateTime CreatedAt; decimal Total; List<OrderLine> Lines }`
- [ ] `Models.cs` with `OrderLine { int Id; int OrderId; int ProductId; string ProductName; decimal UnitPrice; int Quantity }`
- [ ] `OrderingDbContext.cs` with `DbSet<Order> Orders`, `DbSet<OrderLine> OrderLines`, the one-to-many relationship, string lengths and money precision
- [ ] `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")` in `Program.cs`
- [ ] `EnsureCreated()` at startup, no seed
- [ ] AppHost: `ordering-api` gets `WithReference(orderingDb).WaitFor(orderingDb)`

## Data model
Table `Orders` in `orderingdb` (the EF default name from `DbSet<Order> Orders`; SQL must quote it as `"Orders"`):

| Property | CLR type | Postgres column | Constraints |
|---|---|---|---|
| `Id` | `int` | `integer`, identity | primary key |
| `BuyerId` | `string` | `character varying(100)` | NOT NULL |
| `CreatedAt` | `DateTime` (UTC) | `timestamp with time zone` | NOT NULL |
| `Total` | `decimal` | `numeric(18,2)` | NOT NULL |

Table `OrderLines` in `orderingdb` (from `DbSet<OrderLine> OrderLines`; quote as `"OrderLines"`):

| Property | CLR type | Postgres column | Constraints |
|---|---|---|---|
| `Id` | `int` | `integer`, identity | primary key |
| `OrderId` | `int` | `integer` | NOT NULL, FK → `"Orders"."Id"`, ON DELETE CASCADE, indexed (EF default) |
| `ProductId` | `int` | `integer` | NOT NULL (no FK: Catalog owns products) |
| `ProductName` | `string` | `character varying(100)` | NOT NULL |
| `UnitPrice` | `decimal` | `numeric(18,2)` | NOT NULL |
| `Quantity` | `int` | `integer` | NOT NULL |

`Order` and `OrderLine` stay plain classes. The max lengths, the required flags, the money precision and the relationship are configured in `OrderingDbContext.OnModelCreating`. `CreatedAt` uses Npgsql's default `DateTime` mapping (no value converter); ordering-api/02 sets it with `DateTime.UtcNow`.

## Tasks
1. `Models.cs`: add the `Order` and `OrderLine` classes with the properties above. `BuyerId` and `ProductName` default to `string.Empty`, and `Lines` defaults to an empty list (`= []`).
2. `OrderingDbContext.cs`: add `OrderingDbContext(DbContextOptions<OrderingDbContext>) : DbContext` (primary constructor) with `DbSet<Order> Orders` and `DbSet<OrderLine> OrderLines`. `OnModelCreating`:
   - `Order`: `BuyerId` required with max length 100, `Total` precision (18, 2), and `HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade)`.
   - `OrderLine`: `ProductName` required with max length 100, `UnitPrice` precision (18, 2).
3. `Program.cs`: call `builder.AddNpgsqlDbContext<OrderingDbContext>("orderingdb")` after `AddServiceDefaults()`. After `builder.Build()`, create a service scope, resolve `OrderingDbContext` and `await db.Database.EnsureCreatedAsync()`, then do the existing pipeline/endpoint mapping and `app.Run()`. Keep the `GET /` placeholder.
4. `AppHost.cs`: chain `.WithReference(orderingDb).WaitFor(orderingDb)` onto the `ordering-api` registration. The other resources don't change.

## Files touched
- `src/EShopClaude.Ordering.Api/Models.cs` (new)
- `src/EShopClaude.Ordering.Api/OrderingDbContext.cs` (new)
- `src/EShopClaude.Ordering.Api/Program.cs` (edit)
- `src/EShopClaude.AppHost/AppHost.cs` (edit: only the `ordering-api` registration)

No changes to the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No endpoints: create is ordering-api/02, get is ordering-api/03, list is ordering-api/04.
- No `Total` computation or input validation (ordering-api/02).
- No order status, lifecycle, cancellation or editing.
- No `BuyerId` index or any index beyond EF's FK index.
- No FK or lookup from `OrderLines.ProductId` to Catalog.
- No seed, no migrations, no data volume.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0 (there are no tests yet; this confirms the suite still builds).
3. From `src/`: `aspire run`. On the dashboard Resources page, `postgres`, the pgAdmin container, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api` and `webfrontend` are all Running and Healthy.
4. On the dashboard: while `orderingdb` is not yet Healthy, `ordering-api` shows the state **Waiting**. It moves to Starting/Running only after `orderingdb` is Healthy.
5. Dashboard → `ordering-api` → resource details → Environment variables: `ConnectionStrings__orderingdb` is present.
6. Dashboard → `ordering-api` → Console logs: EF Core `CREATE TABLE "Orders"` and `CREATE TABLE "OrderLines"` commands are logged, and there are no exceptions.
7. Request `GET <ordering-api endpoint>/health` (the URL is on the dashboard) → `200` with body `Healthy`.
8. Request `GET <ordering-api endpoint>/` → `200` with body `Ordering API is running.`
9. Open pgAdmin from the dashboard → `orderingdb` → Query Tool, then run
   `SELECT column_name, data_type, character_maximum_length, numeric_precision, numeric_scale, is_nullable FROM information_schema.columns WHERE table_name = 'Orders' ORDER BY ordinal_position;`
   → exactly 4 rows:
   | column_name | data_type | character_maximum_length | numeric_precision | numeric_scale | is_nullable |
   |---|---|---|---|---|---|
   | Id | integer | | 32 | 0 | NO |
   | BuyerId | character varying | 100 | | | NO |
   | CreatedAt | timestamp with time zone | | | | NO |
   | Total | numeric | | 18 | 2 | NO |
10. Same query with `table_name = 'OrderLines'` → exactly 6 rows:
    | column_name | data_type | character_maximum_length | numeric_precision | numeric_scale | is_nullable |
    |---|---|---|---|---|---|
    | Id | integer | | 32 | 0 | NO |
    | OrderId | integer | | 32 | 0 | NO |
    | ProductId | integer | | 32 | 0 | NO |
    | ProductName | character varying | 100 | | | NO |
    | UnitPrice | numeric | | 18 | 2 | NO |
    | Quantity | integer | | 32 | 0 | NO |
11. pgAdmin Query Tool on `orderingdb`:
    `SELECT conname, confrelid::regclass, confdeltype FROM pg_constraint WHERE conrelid = '"OrderLines"'::regclass AND contype = 'f';`
    → exactly 1 row: `FK_OrderLines_Orders_OrderId`, `"Orders"`, `c` (cascade).
12. pgAdmin Query Tool on `orderingdb`: `SELECT (SELECT count(*) FROM "Orders"), (SELECT count(*) FROM "OrderLines");` → `0`, `0`.
13. `git diff --stat main` → only this spec and the 4 files listed in Files touched.

## References
- PROJECT.md §3: AppHost resources (`ordering-api` → `orderingdb`, `WaitFor` on each reference) and "Code style per API" (flat files, `decimal` for money).
- PROJECT.md §4.3: Ordering.Api data (`Order`, `OrderLine`, lines copy name and price, no status) and startup (`EnsureCreated()`, no seed).
- PROJECT.md §2 "Out of scope": no EF Core migrations, no persistent data volumes, no order status/lifecycle.
- PLAN.md 3.2: Ordering.Api.
- catalog-api/01-product-data.md: the pattern this spec mirrors.
