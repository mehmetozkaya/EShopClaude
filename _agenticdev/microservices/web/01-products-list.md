# web 01 — products list

**Status:** Not started

## Goal
Show the product catalog on the storefront home page (use case 1). This is the first page that calls a service. It adds the `catalog-api` reference to `webfrontend`, a `CatalogApiClient` with one list method, and renames the nav's Home link to **Products**.

The client, the page, the nav rename and the AppHost reference are one spec because none of them can be verified on its own. The page is the only way to see the client work, and the reference exists only for the client.

## Depends on
- catalog-api/03-list-products

## Scope
- [ ] `AppHost.cs`: on `webfrontend`, add `.WithReference(catalogApi).WaitFor(catalogApi)`.
- [ ] `CatalogApiClient.cs`: a typed `HttpClient` with base address `https+http://catalog-api` and a method `GetProductsAsync()` → `GET /products`. It declares its own local `ProductDto(int Id, string Name, string Description, decimal Price)` record.
- [ ] `Program.cs`: register `CatalogApiClient` with `AddHttpClient`.
- [ ] `Home.razor` at `/`: a table with the name, description and price of each product, in the order Catalog returns. It has a page title of "Products".
- [ ] `NavMenu.razor`: rename the Home link to **Products** (`/`).

## Explicit non-goals
- No **Add to cart** button, no `BuyerId` constant and no `BasketApiClient` (web/02).
- No Cart or Orders pages or nav links (web/03, web/07).
- No `basket-api` or `ordering-api` reference on `webfrontend`. No spec adds a reference it does not use.
- No product detail page, images, categories, search or paging (PROJECT.md "Out of scope").
- Prices come only from what Basket and Ordering return, except that this page shows the price from Catalog's list. No other page asks Catalog for a price again, and no page sends a price to a service.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database. Web gets no EF Core, Npgsql or Redis package.
- Nothing from the PROJECT.md "Out of scope" list is added. There is no shared contracts library and no retries beyond ServiceDefaults.
- No changes to Catalog.Api, Basket.Api, Ordering.Api or ServiceDefaults. In `AppHost.cs`, only the `webfrontend` registration changes.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy. That means `postgres`, pgAdmin, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api` and `webfrontend`. On the dashboard, `webfrontend` shows a reference to `catalog-api` and none to `basket-api` or `ordering-api`.
4. Browser: open the `webfrontend` URL from the dashboard. `/` shows 8 rows, in this order: Wireless Mouse 24.99, Mechanical Keyboard 89.00, USB-C Hub 39.50 and so on, through Desk Mat 19.90. Each row has its description.
5. HTTP: `GET http://localhost:5101/products` returns the same 8 products in the same order as the page.
6. Browser: the nav shows one link, **Products**. There is no Home, Weather or Counter.
7. Trace: on the dashboard's Traces page, the trace for loading `/` has a `webfrontend` span with a child `GET /products` span on `catalog-api`.

## References
