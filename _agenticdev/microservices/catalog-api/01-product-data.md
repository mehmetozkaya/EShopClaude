# catalog-api 01 — product data

**Status:** Not started

## Goal
Give Catalog.Api its own persistence: the `Product` entity stored in `catalogdb`, with the schema created at startup. Every later catalog spec reads from this table.

The entity, the DbContext and the AppHost wiring are one spec because none of them can be verified alone: the context can't connect without the AppHost reference, and the model is only visible through the context (the table in pgAdmin).

## Depends on
Nothing.

## Scope
- [ ] `Models.cs` with `Product { int Id; string Name; string Description; decimal Price }`
- [ ] `CatalogDbContext.cs` with `DbSet<Product> Products` and the price column precision
- [ ] `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")` in `Program.cs`
- [ ] `EnsureCreated()` at startup
- [ ] AppHost: `catalog-api` gets `WithReference(catalogDb).WaitFor(catalogDb)`

## Explicit non-goals
- No seed data (catalog-api/02).
- No endpoints (catalog-api/03, 04).
- No migrations, no data volume.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy, and catalog-api starts only after catalogdb.
- pgAdmin: `catalogdb` has an empty `Products` table with `Id`, `Name`, `Description` and `Price` columns.

## References
