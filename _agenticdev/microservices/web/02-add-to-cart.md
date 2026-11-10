# web 02 — add to cart

**Status:** Done (344e40b)

## Goal
Let the buyer put a product in the basket from the Products page (use case 2). This spec adds the `BuyerId = "demo-user"` constant in one place in Web, a `BasketApiClient` with one add method, and the `basket-api` reference on `webfrontend`. The page sends only a product id and a quantity. Basket.Api takes the name and price snapshot from Catalog.

The constant, the client, the button and the AppHost reference are one spec because none of them can be verified on its own. The button is the only way to see the client work, and the reference exists only for the client.

## Depends on
- web/01-products-list
- basket-api/02-add-item

## Scope
- [ ] `AppHost.cs`: capture the `basket-api` registration in a variable (`var basketApi = builder.AddProject<Projects.EShopClaude_Basket_Api>("basket-api")`), leaving its chain unchanged. On `webfrontend`, add `.WithReference(basketApi).WaitFor(basketApi)`. Keep the `catalog-api` reference.
- [ ] `Buyer.cs` (Web project root): the single place the buyer is defined:
  ```csharp
  public static class Buyer { public const string Id = "demo-user"; }
  ```
- [ ] `BasketApiClient.cs` (Web project root, next to `CatalogApiClient.cs`): a typed `HttpClient` with a method `Task AddItemAsync(string buyerId, int productId, int quantity, CancellationToken ct = default)` → `POST /baskets/{buyerId}/items` via `PostAsJsonAsync`, then `EnsureSuccessStatusCode()`. It declares its own local request record:
  ```csharp
  public record AddItemRequest(int ProductId, int Quantity);
  ```
  No basket DTO yet (web/03 adds it).
- [ ] `Program.cs`: register `BasketApiClient` with `AddHttpClient<BasketApiClient>(c => c.BaseAddress = new("https+http://basket-api"))`.
- [ ] `Home.razor`:
  - Add `@rendermode InteractiveServer` (prerendering stays on, the default).
  - Add a last table column with no header text holding `<button class="btn btn-primary btn-sm">Add to cart</button>` on each row. No disabling while a request is in flight.
  - Clicking it calls `AddItemAsync(Buyer.Id, p.Id, 1)`, then shows `<p class="alert alert-success">Added {name}</p>` above the table and stays on `/`. Each click replaces the message. It never auto-hides.
  - No error handling: if the call fails, the exception propagates and Blazor shows its error UI.

## Explicit non-goals
- No `/cart` page, no Cart nav link and no get-basket method or basket DTO (web/03).
- No remove or checkout (web/04, web/06).
- No `ordering-api` reference on `webfrontend` (web/05).
- No quantity picker. Every click adds 1, and re-adding increments the line in Basket (PROJECT.md "Out of scope": cart quantity editor).
- No cart badge or item counter in the nav.
- Prices come only from what Basket and Ordering return. No page asks Catalog for a price again, and the add request sends no name or price.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database or Redis.
- Nothing from the PROJECT.md "Out of scope" list is added. There is no login, no shared contracts library and no retries beyond ServiceDefaults.
- No changes to Catalog.Api, Basket.Api, Ordering.Api or ServiceDefaults. In `AppHost.cs`, only the `webfrontend` registration changes, plus capturing `basket-api` in a `basketApi` variable.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy, and `webfrontend` references `catalog-api` and `basket-api` but not `ordering-api`.
4. Browser: on a fresh `aspire run`, click **Add to cart** on Wireless Mouse. The page shows "Added Wireless Mouse", and the URL is still `/`.
5. HTTP: `GET http://localhost:5102/baskets/demo-user` → `200` with one line `{1, "Wireless Mouse", 24.99, 1}`.
6. Browser: click **Add to cart** on Wireless Mouse again, then on Mechanical Keyboard. `GET http://localhost:5102/baskets/demo-user` returns Wireless Mouse with quantity 2 and Mechanical Keyboard 89.00 with quantity 1.
7. Trace: the trace for one click goes `webfrontend` → `basket-api` `POST /baskets/{buyerId}/items` → `catalog-api` `GET /products/{id}`, and has a Redis `SET` span.
8. `grep -rn "demo-user" src/EShopClaude.Web --include=*.cs --include=*.razor` → exactly one match, the constant in `Buyer.cs`.

## References
- PROJECT.md §2: use case 2, "Add to cart".
- PROJECT.md §3: AppHost resources (`webfrontend` → `basket-api`, `WaitFor`), `https+http://<resource-name>` service discovery, and the buyer constant defined only in Web.
- PROJECT.md §4.2: `POST /baskets/{buyerId}/items` with `{ productId, quantity }`, snapshot taken from Catalog.
- PROJECT.md §5: route `/`, the Add to cart button (quantity 1, stays on page, "Added {name}"), typed clients with local DTOs.
- PLAN.md Step 5: storefront spec backlog. `02` adds the `basket-api` reference.
