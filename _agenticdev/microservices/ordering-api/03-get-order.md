# ordering-api 03 — get order

**Status:** Not started

## Goal
Return one order with its lines via `GET /orders/{id}`. The storefront's order detail page, which is also the checkout confirmation (use case 4), reads this endpoint.

## Depends on
ordering-api/02-create-order

## Scope
- [ ] `GET /orders/{id}` returns `200 Order` with its lines, or `404`
- [ ] Add the request to `Ordering.Api.http`

## Explicit non-goals
- No order list (ordering-api/04).
- No status or editing.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- HTTP: POST a sample order, then GET it by the returned id: `200` with the lines, `CreatedAt` and `Total`.
- HTTP: an unknown id returns `404`.

## References
