# ordering-api 04 — list orders

**Status:** Not started

## Goal
Return a buyer's orders as summaries via `GET /orders?buyerId=`. The storefront's "My orders" page (use case 4) reads this endpoint.

## Depends on
ordering-api/02-create-order

## Scope
- [ ] `GET /orders?buyerId=` returns `200 OrderSummary[] { id, createdAt, total, itemCount }`
- [ ] Only the given buyer's orders, newest first
- [ ] Add the request to `Ordering.Api.http`

## Explicit non-goals
- No paging or filtering beyond `buyerId`.
- No lines in the summary (ordering-api/03 returns them).

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- HTTP: POST two orders for one buyer and one for another; the list for the first buyer shows only its two, newest first, with the correct `itemCount` and `total`.
- HTTP: a buyer with no orders gets `200 []`.

## References
