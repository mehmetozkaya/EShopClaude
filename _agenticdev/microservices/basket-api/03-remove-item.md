# basket-api 03 — remove item

**Status:** Not started

## Goal
Let a buyer remove a line from the basket (use case 2). It comes after add-item so that removing a real line can be verified.

## Depends on
basket-api/02-add-item

## Scope
- [ ] `DELETE /baskets/{buyerId}/items/{productId}` removes the line, saves, and returns `200 Basket`
- [ ] A missing line returns `200` with the basket unchanged
- [ ] Add the DELETE request to `Basket.Api.http`

## Explicit non-goals
- No decrement of quantity; remove deletes the whole line.
- No checkout (basket-api/04).
- No calls to other services.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- HTTP: add two products, delete one: the response and a follow-up GET show only the other line.
- HTTP: deleting a product that isn't in the basket returns `200` with the basket unchanged.

## References
