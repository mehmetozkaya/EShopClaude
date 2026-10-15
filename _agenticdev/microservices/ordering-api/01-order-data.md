# ordering-api 01 — order data

**Status:** Not started

## Goal
Give Ordering.Api its own persistence: orders and their lines in `orderingdb`, with the schema created at startup. Lines copy the product name and price, so an order never depends on Catalog.

The entities, the DbContext and the AppHost wiring are one spec because none of them can be verified alone: the context can't connect without the AppHost reference, and the model is only visible through the context (the tables in pgAdmin).

## Depends on
Nothing.

## Scope
- [ ] `Models.cs` with `Order { int Id; string BuyerId; DateTime CreatedAt; decimal Total; List<OrderLine> Lines }`
- [ ] `Models.cs` with `OrderLine { int Id; int OrderId; int ProductId; string ProductName; decimal UnitPrice; int Quantity }`
- [ ] `OrderingDbContext.cs`: `Order` owns `Lines`; precision set on the money columns
- [ ] `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")` in `Program.cs`
- [ ] `EnsureCreated()` at startup, no seed
- [ ] AppHost: `ordering-api` gets `WithReference(orderingDb).WaitFor(orderingDb)`

## Explicit non-goals
- No endpoints (ordering-api/02–04).
- No order status or lifecycle.
- No seed, no migrations, no data volume.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy, and ordering-api starts only after orderingdb.
- pgAdmin: `orderingdb` has empty `Orders` and `OrderLines` tables with the expected columns.

## References
