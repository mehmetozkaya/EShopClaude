# web 07 — orders list

**Status:** Done (c4f5663)

## Goal
Show the buyer's orders on `/orders` (use case 4). The page is a table of id, date, item count and total, newest first. Each row links to its detail page from web/05. When there are no orders, the page shows an empty state. This spec adds the **Orders** nav link, which completes the storefront journey.

## Depends on
- web/02-add-to-cart (the `BuyerId` constant)
- web/05-order-detail (`OrderingApiClient` and the detail page the rows link to)
- ordering-api/04-list-orders

## Scope
- [ ] `OrderingApiClient.cs`: add `Task<OrderSummaryDto[]> GetOrdersAsync(string buyerId, CancellationToken ct = default)` → `GetFromJsonAsync<OrderSummaryDto[]>($"/orders?buyerId={Uri.EscapeDataString(buyerId)}", ct) ?? []`. Failures propagate, the same as the other clients. It declares a local record `OrderSummaryDto(int Id, DateTime CreatedAt, decimal Total, int ItemCount)`. `GetOrderAsync` is unchanged.
- [ ] `Orders.razor` at `/orders`: `@rendermode InteractiveServer` with a "Loading…" state, as in `OrderDetail.razor`. It loads in `OnInitializedAsync` with `Ordering.GetOrdersAsync(Buyer.Id)`. It has `<PageTitle>My orders</PageTitle>` and `<h1>My orders</h1>`, then a table with the columns Order, Date, Items and Total, in the order Ordering returns (newest first):
  - Order: the id as a link, `<a href="orders/{id}">{id}</a>`. There is no whole-row click handler.
  - Date: `CreatedAt` (UTC) formatted `yyyy-MM-dd HH:mm 'UTC'` with invariant culture, with no local-time conversion.
  - Items: `ItemCount` as Ordering returns it.
  - Total: `Total` as Ordering returns it, formatted by a page-local `Money()` helper (`0.00` invariant) copied from `OrderDetail.razor`. There is no shared helper.
- [ ] Empty state in `Orders.razor`: when the list is empty, show "You have no orders yet. `<a href="">Browse products</a>`." instead of the table.
- [ ] `NavMenu.razor`: add an **Orders** `NavLink` (`href="orders"`, icon `bi bi-receipt`) after Cart.
- [ ] `NavMenu.razor.css`: add a `.bi-receipt` rule with the Bootstrap Icons `receipt` SVG as a white `data:` URI `background-image`, the same way as the existing `.bi-*` rules. The template doesn't load the Bootstrap Icons font, so without this rule the icon renders blank.

## Explicit non-goals
- No paging, filtering or sorting controls (PROJECT.md "Out of scope").
- No status, cancellation or editing of orders.
- No AppHost change. `webfrontend` already references `ordering-api`.
- Prices come only from what Basket and Ordering return. The page shows the stored `total` and `itemCount`, doesn't recompute them and doesn't call Catalog.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api, ServiceDefaults or `AppHost.cs`.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy.
4. Browser: on a fresh `aspire run`, open `/orders`. It shows the empty state.
5. Browser: place order A (Wireless Mouse × 1, total 24.99), then order B (Mechanical Keyboard × 2, total 178.00). `/orders` shows two rows, B first and then A, with item counts 2 and 1 and totals 178.00 and 24.99.
6. HTTP: `GET http://localhost:5103/orders?buyerId=demo-user` returns the same ids, item counts and totals in the same order.
7. HTTP: create an order for another buyer (`POST http://localhost:5102/baskets/other-buyer/items` `{ "productId": 1, "quantity": 1 }`, then `POST http://localhost:5102/baskets/other-buyer/checkout`). After a refresh, `/orders` still shows only orders A and B.
8. Browser: clicking the order id link for B opens `/orders/{B}` with its lines and total.
9. Trace: the trace for loading `/orders` has `webfrontend` → `ordering-api` `GET /orders` with a Postgres span. It has no span on `catalog-api` or `basket-api`.
10. Browser: the nav shows **Products**, **Cart** and **Orders**. Full journey: list → add 2 products → remove one → place order → order detail → **Orders** shows it → **Cart** is empty.

## References
- PROJECT.md §2: use case 4, "See the order (list of my orders + order detail)".
- PROJECT.md §2 "Out of scope": no paging, no order status/lifecycle, cancellation or editing, and no login or users.
- PROJECT.md §3: the buyer id `demo-user` is defined only in Web and passed to the APIs in the URL.
- PROJECT.md §4.3: `GET /orders?buyerId=` → `200 OrderSummary[] { id, createdAt, total, itemCount }`, newest first.
- PROJECT.md §5: nav **Products** | **Cart** | **Orders**; route `/orders` (My orders: id, date, item count, total, each linking to its detail, with an empty state); typed `OrderingApiClient` with local DTO records; the user journey.
- PLAN.md Step 5: storefront spec backlog (`07-orders-list`). Each typed client grows one method per spec, and `05` already added `ordering-api` to `webfrontend`.
