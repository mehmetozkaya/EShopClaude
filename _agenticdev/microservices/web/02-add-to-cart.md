# web 02 — add to cart

**Status:** Not started

## Goal
Let the buyer put a product in the basket from the Products page (use case 2). This spec adds the `BuyerId = "demo-user"` constant in one place in Web, a `BasketApiClient` with one add method, and the `basket-api` reference on `webfrontend`. The page sends only a product id and a quantity. Basket.Api takes the name and price snapshot from Catalog.

## Depends on
- web/01-products-list
- basket-api/02-add-item

## Scope
- [ ] `AppHost.cs`: on `webfrontend`, add `.WithReference(basketApi).WaitFor(basketApi)`. Keep the `catalog-api` reference.
- [ ] Add a `BuyerId = "demo-user"` constant in one place in Web.
- [ ] `BasketApiClient.cs`: a typed `HttpClient` with base address `https+http://basket-api` and a method `AddItemAsync(buyerId, productId, quantity)` → `POST /baskets/{buyerId}/items` with body `{ productId, quantity }`. It declares its own local DTO records.
- [ ] `Program.cs`: register `BasketApiClient`.
- [ ] `Home.razor`: an **Add to cart** button on each row (quantity 1). Clicking it calls `AddItemAsync`, then shows "Added {name}" and stays on `/`. The page uses interactive server rendering.

## Explicit non-goals
- No `/cart` page, no Cart nav link and no get-basket method (web/03).
- No remove or checkout (web/04, web/06).
- No `ordering-api` reference on `webfrontend` (web/05).
- No quantity picker. Every click adds 1, and re-adding increments the line in Basket (PROJECT.md "Out of scope": cart quantity editor).
- No cart badge or item counter in the nav.
- Prices come only from what Basket and Ordering return. No page asks Catalog for a price again, and the add request sends no name or price.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database or Redis.
- Nothing from the PROJECT.md "Out of scope" list is added. There is no login, no shared contracts library and no retries beyond ServiceDefaults.
- No changes to Catalog.Api, Basket.Api, Ordering.Api or ServiceDefaults. In `AppHost.cs`, only the `webfrontend` registration changes.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy, and `webfrontend` references `catalog-api` and `basket-api` but not `ordering-api`.
4. Browser: on a fresh `aspire run`, click **Add to cart** on Wireless Mouse. The page shows "Added Wireless Mouse", and the URL is still `/`.
5. HTTP: `GET http://localhost:5102/baskets/demo-user` → `200` with one line `{1, "Wireless Mouse", 24.99, 1}`.
6. Browser: click **Add to cart** on Wireless Mouse again, then on Mechanical Keyboard. `GET http://localhost:5102/baskets/demo-user` returns Wireless Mouse with quantity 2 and Mechanical Keyboard 89.00 with quantity 1.
7. Trace: the trace for one click goes `webfrontend` → `basket-api` `POST /baskets/{buyerId}/items` → `catalog-api` `GET /products/{id}`, and has a Redis `SET` span.
8. `grep -rn "demo-user" src/EShopClaude.Web --include=*.cs --include=*.razor` → exactly one match, the constant.

## References
