# web 05 — order detail

**Status:** Not started

## Goal
Show one order on `/orders/{id:int}` (use case 4). The page shows the order's id, date, lines and total. It is also the confirmation page that Place order (web/06) navigates to. It comes before Place order so that checkout lands on a working page. It can be verified on its own by creating an order over HTTP through basket-api checkout. This is the first page that calls Ordering, so it adds the `ordering-api` reference on `webfrontend` and an `OrderingApiClient` with one get method.

## Depends on
- ordering-api/03-get-order
- No earlier web spec. The page doesn't use the `BuyerId` constant or the basket client.

## Scope
- [ ] `AppHost.cs`: on `webfrontend`, add `.WithReference(orderingApi).WaitFor(orderingApi)`. Keep the existing references.
- [ ] `OrderingApiClient.cs`: a typed `HttpClient` with base address `https+http://ordering-api` and a method `Task<OrderDto?> GetOrderAsync(int id, CancellationToken ct = default)` → `GET /orders/{id}`. It calls `http.GetAsync`. On `404` it returns `null`. Otherwise it calls `EnsureSuccessStatusCode()` and returns `ReadFromJsonAsync<OrderDto>(ct)`. Other failures propagate (same as web/02–04). It declares its own local records:
  - `OrderDto(int Id, string BuyerId, DateTime CreatedAt, decimal Total, List<OrderLineDto> Lines)`
  - `OrderLineDto(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity)`
- [ ] `Program.cs`: register `OrderingApiClient`.
- [ ] `OrderDetail.razor` at `/orders/{id:int}`: `@rendermode InteractiveServer` with a "Loading…" state, as in `Cart.razor`. `<PageTitle>Order {id}</PageTitle>` and `<h1>Order {id}</h1>`. It shows:
  - the created date: `CreatedAt` (UTC) formatted `yyyy-MM-dd HH:mm 'UTC'` with invariant culture and no local-time conversion;
  - a lines table (name, unit price, qty, line total);
  - the order `Total` as Ordering stored it.

  Money is formatted `0.00` invariant by a page-local `Money()` helper copied from `Cart.razor` (no shared helper, and `Cart.razor` is not changed).
- [ ] Not-found state: when Ordering returns `404`, show "Order {id} not found" with a link to Products.

## Explicit non-goals
- No `/orders` list page and no Orders nav link (web/07).
- No **Place order** button (web/06).
- No status, cancellation or editing (PROJECT.md "Out of scope": order lifecycle).
- No check that the order belongs to `demo-user` (no auth, PROJECT.md "Out of scope").
- Prices come only from what Basket and Ordering return. The page shows the order's stored prices and `Total`, doesn't recompute `Total` and doesn't call Catalog.
- Checkout is one call to Basket. No page calls Ordering to create anything, and `OrderingApiClient` has no POST.
- No page reads a database.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api or ServiceDefaults. In `AppHost.cs`, only the `webfrontend` registration changes.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy, and `webfrontend` references `catalog-api`, `basket-api` and `ordering-api`.
4. HTTP: on a fresh `aspire run`, create an order. Send `POST http://localhost:5102/baskets/demo-user/items` with `{ "productId": 1, "quantity": 2 }` and then with `{ "productId": 3, "quantity": 1 }`. Then send `POST http://localhost:5102/baskets/demo-user/checkout` → `200 { "orderId": N }`.
5. Browser: open `/orders/N`. It shows order N, its date, Wireless Mouse 24.99 × 2 = 49.98, USB-C Hub 39.50 × 1 = 39.50, and a total of 89.48.
6. HTTP: `GET http://localhost:5103/orders/N` returns the same lines and `total` as the page.
7. Browser: `/orders/99999` shows "Order 99999 not found". `/orders/abc` shows the app's routing not-found page.
8. Trace: the trace for loading `/orders/N` has `webfrontend` → `ordering-api` `GET /orders/{id}` with a Postgres span. It has no span on `catalog-api` or `basket-api`.
9. pgAdmin: in `orderingdb`, the order row N has total 89.48 and two order line rows.

## References
- PROJECT.md §2: use case 4, "See the order (… order detail)".
- PROJECT.md §2 "Out of scope": no order status/lifecycle, cancellation or editing; no login or users.
- PROJECT.md §4.3: `GET /orders/{id}` → `200 Order` with lines, or `404`; lines copy name and price; `Total` is stored on create.
- PROJECT.md §4.4 step 7: after checkout, Web navigates to `/orders/{orderId}`.
- PROJECT.md §5: route `/orders/{id:int}` (order id, date, lines, total; also the confirmation page after checkout); typed `OrderingApiClient` with local DTO records.
- PLAN.md Step 5: storefront spec backlog (`05-order-detail`); `05` adds the `ordering-api` reference on `webfrontend`; order detail comes before Place order.
