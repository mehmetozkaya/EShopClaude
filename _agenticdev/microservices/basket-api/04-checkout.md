# basket-api 04 — checkout

**Status:** Not started

## Goal
Turn the basket into an order (use case 3). Basket.Api orchestrates checkout per PROJECT.md §4.4: it sends the snapshot lines to Ordering, then clears the basket.

## Depends on
- basket-api/02-add-item
- ordering-api/02-create-order

## Scope
- [ ] Typed `OrderingClient` with base address `https+http://ordering-api`
- [ ] Store delete on `basket:{buyerId}`
- [ ] `POST /baskets/{buyerId}/checkout`:
  - [ ] Missing or empty basket returns `400`
  - [ ] POST `/orders` with buyerId and lines mapped 1:1 from the basket
  - [ ] Delete the basket, then return `200 { orderId }`
  - [ ] If the order call fails, the error propagates and the basket is kept
- [ ] AppHost: `basket-api` gets `WithReference(orderingApi).WaitFor(orderingApi)`
- [ ] Add the checkout request to `Basket.Api.http`

## Explicit non-goals
- No re-pricing at checkout.
- No compensation, retries or fallbacks beyond ServiceDefaults.
- No payment, shipping or checkout form.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy, and basket-api starts only after ordering-api.
- HTTP: checkout of an empty basket returns `400`.
- HTTP: checkout of a filled basket returns `200 { orderId }`, and `GET /baskets/{buyerId}` is empty afterwards.
- pgAdmin: `orderingdb` has the order with lines matching the basket and the correct `Total`. If ordering-api/03 is merged, `GET ordering-api /orders/{orderId}` shows the same.
- Trace: basket-api → ordering-api → postgres, plus the Redis delete.
- Failure check: stop ordering-api from the dashboard, run checkout: the call fails and the basket is still there.

## References
