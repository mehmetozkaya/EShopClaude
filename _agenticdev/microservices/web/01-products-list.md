# web 01 — products list

**Status:** Not started

## Goal
Show the product catalog on the storefront home page (use case 1). This is the first page that calls a service. It adds the `catalog-api` reference to `webfrontend`, a `CatalogApiClient` with one list method, and renames the nav's Home link to **Products**.

The client, the page, the nav rename and the AppHost reference are one spec because none of them can be verified on its own. The page is the only way to see the client work, and the reference exists only for the client.

## Depends on
- catalog-api/03-list-products

## Scope
- [ ] `AppHost.cs`: on `webfrontend`, add `.WithReference(catalogApi).WaitFor(catalogApi)`.
- [ ] `CatalogApiClient.cs` (Web project root, where the template's `WeatherApiClient.cs` was): a typed `HttpClient` with a method `Task<ProductDto[]> GetProductsAsync(CancellationToken ct = default)` → `GET /products` via `GetFromJsonAsync`, returning `[]` when the body is null. It declares its own local record:
  ```csharp
  public record ProductDto(int Id, string Name, string Description, decimal Price);
  ```
- [ ] `Program.cs`: register `CatalogApiClient` with `AddHttpClient<CatalogApiClient>(c => c.BaseAddress = new("https+http://catalog-api"))`.
- [ ] `Home.razor` at `/`:
  - Static SSR (no `@rendermode`). Load the products in `OnInitializedAsync`.
  - `<PageTitle>Products</PageTitle>` and `<h1>Products</h1>`. Remove the template welcome text.
  - While the list is null, show "Loading…". If it is empty, show "No products.".
  - Otherwise show a table with columns Name, Description and Price, in the order Catalog returns. Format the price as `Price.ToString("0.00", CultureInfo.InvariantCulture)`.
  - No error handling: if the call fails, the exception propagates.
- [ ] `NavMenu.razor`: change the Home link's text to **Products** (`/`). Keep the `bi-house-door-fill` icon.

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
- PROJECT.md §2: use case 1, "List products".
- PROJECT.md §3: AppHost resources (`webfrontend` → `catalog-api`, `WaitFor`) and `https+http://<resource-name>` service discovery.
- PROJECT.md §4.1: `GET /products` → `200 Product[]` ordered by Id.
- PROJECT.md §5: route `/`, the Products page, the nav menu and typed clients with local DTOs.
- PLAN.md Step 5: storefront spec backlog. `01` adds the `catalog-api` reference.
