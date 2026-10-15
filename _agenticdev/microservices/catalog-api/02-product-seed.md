# catalog-api 02 — product seed

**Status:** Not started

## Goal
Fill the read-only catalog with demo products, so the storefront and the basket have something to list and add. The catalog has no admin, so the seed is the only source of products.

## Depends on
catalog-api/01-product-data

## Scope
- [ ] Code seed of about 8 products, run at startup after `EnsureCreated()`
- [ ] Seed only when the `Products` table is empty
- [ ] Prices as `decimal`

## Explicit non-goals
- No endpoints (catalog-api/03, 04).
- No categories, images or stock.
- No data volume; data resets on AppHost restart by design.

## Definition of Done
- `dotnet build EShopClaude.sln` passes.
- `aspire run`: every resource is healthy.
- pgAdmin: `catalogdb.Products` has about 8 rows with names, descriptions and prices.
- Restart the AppHost: the products are seeded again, with no duplicates.

## References
