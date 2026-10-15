# basket-api 02 — add item

**Status:** Not started

## Goal
Let a buyer add a product to the basket (use case 2). Basket.Api looks the product up in Catalog and snapshots its name and price into the line, so the basket price is final from then on.

## Depends on
- basket-api/01-get-basket
- catalog-api/04-get-product

## Scope
- [ ] Typed `CatalogClient` with base address `https+http://catalog-api`
- [ ] Store save on `basket:{buyerId}` (JSON, no TTL)
- [ ] `POST /baskets/{buyerId}/items` with body `{ productId, quantity }`
- [ ] Return `404` when Catalog returns `404`
- [ ] Existing line: increment its quantity; new line: snapshot name and price
- [ ] Save and return `200 Basket`
- [ ] AppHost: `basket-api` gets `WithReference(catalogApi).WaitFor(catalogApi)`
- [ ] Add the POST requests to `Basket.Api.http`

## Explicit non-goals
- No remove or checkout (basket-api/03, 04).
- No quantity editor or decrement.
- No retries or fallbacks beyond ServiceDefaults.
- No re-pricing of existing lines.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy, and basket-api starts only after catalog-api.
- HTTP: add product A twice gives one line with quantity 2; add product B gives a second line with Catalog's name and price.
- HTTP: an unknown product id returns `404`, and the basket is unchanged.
- HTTP: `GET /baskets/{buyerId}` returns the saved basket.
- Trace: one trace runs basket-api → catalog-api → postgres, plus a Redis span.

## References
