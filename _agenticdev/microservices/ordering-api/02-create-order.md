# ordering-api 02 — create order

**Status:** Not started

## Goal
Let callers place an order with `POST /orders`. Basket.Api calls this at checkout. Ordering owns the order total, so it computes and stores `Total` itself.

## Depends on
ordering-api/01-order-data

## Scope
- [ ] `POST /orders` with body `{ buyerId, lines: [{ productId, productName, unitPrice, quantity }] }`
- [ ] Set `CreatedAt` to UTC now
- [ ] Compute and store `Total = Σ UnitPrice × Quantity`
- [ ] Return `201 { id }`
- [ ] `Ordering.Api.http` with a sample order

## Explicit non-goals
- No read endpoints (ordering-api/03, 04).
- No calls to Catalog and no re-pricing; the lines are taken as sent.
- No status, cancellation or editing.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- HTTP: POST the sample order and get `201` with an `id`.
- pgAdmin: `Orders` has the row with UTC `CreatedAt` and the correct `Total`; `OrderLines` has its lines.

## References
