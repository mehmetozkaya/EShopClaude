# basket-api 01 — get basket

**Status:** Not started

## Goal
Give Basket.Api its Redis store and its first endpoint, `GET /baskets/{buyerId}`. The storefront's cart page reads the basket through it, and later basket specs build on the store.

The store, the AppHost wiring and the GET endpoint are one spec because a store with no caller has no behaviour to verify. GET is the smallest endpoint that exercises it.

## Depends on
Nothing.

## Scope
- [ ] `Models.cs` with `Basket { string BuyerId; List<BasketItem> Items }` and `BasketItem { int ProductId; string ProductName; decimal UnitPrice; int Quantity }`
- [ ] `AddRedisClient("basket-cache")` in `Program.cs`
- [ ] `BasketStore.cs` with a read on key `basket:{buyerId}` (JSON value)
- [ ] AppHost: `basket-api` gets `WithReference(basketCache).WaitFor(basketCache)`
- [ ] `GET /baskets/{buyerId}` returns `200 Basket`, an empty basket when none exists
- [ ] `Basket.Api.http` with the GET request

## Explicit non-goals
- No store save or delete (basket-api/02, 04).
- No add, remove or checkout endpoints.
- No calls to catalog-api or ordering-api, and no AppHost references to them.
- No TTL.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy, basket-api starts only after basket-cache, and its Redis health check shows on the dashboard.
- HTTP: `GET /baskets/new-buyer` returns `200 { buyerId: "new-buyer", items: [] }`.
- Trace: the request has a Redis span.

## References
