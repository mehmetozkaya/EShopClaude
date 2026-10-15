# catalog-api 03 — list products

**Status:** Not started

## Goal
Expose the catalog to callers with `GET /products`. The storefront's Products page (use case 1) lists products through this endpoint.

## Depends on
catalog-api/02-product-seed

## Scope
- [ ] `GET /products` returns `200 Product[]` ordered by Id
- [ ] `Catalog.Api.http` with the list request

## Explicit non-goals
- No `GET /products/{id}` (catalog-api/04).
- No paging, filtering or search.
- No write endpoints.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- HTTP: `GET /products` returns `200` with the seeded products, ordered by Id.
- Trace: the request span has EF/Npgsql child spans.

## References
