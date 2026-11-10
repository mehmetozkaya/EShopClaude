# web 03 — cart view

**Status:** Not started

## Goal
Show the buyer's basket on `/cart` (use case 2). Each line shows its name, unit price, quantity and line total, and the page shows the basket total. An empty basket shows an empty state that links to Products. This spec adds the **Cart** nav link. Every number comes from the Basket snapshot.

## Depends on
- web/02-add-to-cart (the `BuyerId` constant, `BasketApiClient` and a way to fill the basket)
- basket-api/01-get-basket

## Scope
- [ ] `BasketApiClient.cs`: add two local DTO records next to `AddItemRequest`:
  ```csharp
  public record BasketDto(string BuyerId, List<BasketItemDto> Items);
  public record BasketItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
  ```
- [ ] `BasketApiClient.cs`: add `Task<BasketDto> GetBasketAsync(string buyerId, CancellationToken ct = default)` → `GetFromJsonAsync<BasketDto>($"/baskets/{buyerId}", ct)`, returning `new BasketDto(buyerId, [])` when the body is null. No error handling: failures propagate (same as web/02).
- [ ] `Components/Pages/Cart.razor` at `@page "/cart"`, `@rendermode InteractiveServer`, `<PageTitle>Cart</PageTitle>`, heading `Cart`. Loads `GetBasketAsync(Buyer.Id)` in `OnInitializedAsync` and shows `<p><em>Loading…</em></p>` while the basket is null.
- [ ] `Cart.razor` table: `<table class="table">` with columns Name | Unit price | Qty | Line total (line total = `UnitPrice × Quantity`), and a `<tfoot>` row "Total" with Σ line totals. Every amount is formatted `ToString("0.00", CultureInfo.InvariantCulture)`. It shows only what Basket returned.
- [ ] `Cart.razor` empty state: when `Items` is empty, show `<p>Your cart is empty. <a href="">Browse products</a>.</p>` instead of the table.
- [ ] `Components/Layout/NavMenu.razor`: add a second `nav-item` after Products: `<NavLink class="nav-link" href="cart">` with `<span class="bi bi-list-nested" aria-hidden="true"></span> Cart` (the template CSS defines no cart icon).

## Explicit non-goals
- No **Remove** button (web/04).
- No **Place order** button (web/06).
- No quantity editor (PROJECT.md "Out of scope").
- No Orders pages or `ordering-api` reference (web/05, web/07). No AppHost change in this spec.
- Prices come only from what Basket and Ordering return. This page doesn't call Catalog, and it sends no price to any service.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database or Redis.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api, ServiceDefaults or `AppHost.cs`.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy.
4. Browser: on a fresh `aspire run`, open `/cart`. It shows the empty state, and its link goes to `/`.
5. Browser: on `/`, add Wireless Mouse twice and Mechanical Keyboard once, then click **Cart** in the nav. `/cart` shows Wireless Mouse 24.99 × 2 = 49.98, Mechanical Keyboard 89.00 × 1 = 89.00, and a total of 138.98.
6. HTTP: `GET http://localhost:5102/baskets/demo-user` returns the same lines, prices and quantities as the page.
7. Trace: the trace for loading `/cart` has `webfrontend` → `basket-api` `GET /baskets/{buyerId}`, and has no span on `catalog-api`.
8. Browser: the nav shows **Products** and **Cart**.

## References
- PROJECT.md §2: use case 2, "Add to cart" (the cart view).
- PROJECT.md §4.2: `GET /baskets/{buyerId}` → `200 Basket`, an empty basket if none exists; `Basket`/`BasketItem` shapes.
- PROJECT.md §5: route `/cart` (lines, unit price, qty, line total, basket total, empty state linking to Products), nav menu Products | Cart, typed clients with local DTO records.
- PLAN.md Step 5: storefront spec backlog (`03-cart-view`); no new AppHost reference in this spec.
