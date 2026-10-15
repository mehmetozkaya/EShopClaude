# catalog-api 04 — get product

**Status:** Not started

## Goal
Expose a single product by id. Basket.Api calls this endpoint when an item is added, to check the product exists and to snapshot its name and price.

## Depends on
catalog-api/02-product-seed

## Scope
- [ ] `GET /products/{id}` returns `200 Product`, or `404` when it doesn't exist
- [ ] Add the request to `Catalog.Api.http` (create the file if catalog-api/03 isn't merged yet)

## Explicit non-goals
- No product detail page or extra product fields.
- No write endpoints.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- HTTP: a seeded id returns `200` with that product.
- HTTP: an unknown id (for example `9999`) returns `404`.
- Trace: the request span has EF/Npgsql child spans.

## References
