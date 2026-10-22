# Microservices — spec backlog

Status of every spec. The process (pick, branch, plan, implement, verify, merge, mark Done) is in [WOW.md](../WOW.md).
A spec is ready to start when every spec in its "Depends on" column is Done.

| Service | Spec | Branch | Depends on | Status |
|---|---|---|---|---|
| catalog-api | [01-product-data](catalog-api/01-product-data.md) | `feature/catalog-01-product-data` | nothing | Done |
| catalog-api | [02-product-seed](catalog-api/02-product-seed.md) | `feature/catalog-02-product-seed` | catalog-api/01 | Done |
| catalog-api | [03-list-products](catalog-api/03-list-products.md) | `feature/catalog-03-list-products` | catalog-api/02 | Done |
| catalog-api | [04-get-product](catalog-api/04-get-product.md) | `feature/catalog-04-get-product` | catalog-api/02 | Done |
| ordering-api | [01-order-data](ordering-api/01-order-data.md) | `feature/ordering-01-order-data` | nothing | Done |
| ordering-api | [02-create-order](ordering-api/02-create-order.md) | `feature/ordering-02-create-order` | ordering-api/01 | Done |
| ordering-api | [03-get-order](ordering-api/03-get-order.md) | `feature/ordering-03-get-order` | ordering-api/02 | Done |
| ordering-api | [04-list-orders](ordering-api/04-list-orders.md) | `feature/ordering-04-list-orders` | ordering-api/02 | Not started |
| basket-api | [01-get-basket](basket-api/01-get-basket.md) | `feature/basket-01-get-basket` | nothing | Not started |
| basket-api | [02-add-item](basket-api/02-add-item.md) | `feature/basket-02-add-item` | basket-api/01, catalog-api/04 | Not started |
| basket-api | [03-remove-item](basket-api/03-remove-item.md) | `feature/basket-03-remove-item` | basket-api/02 | Not started |
| basket-api | [04-checkout](basket-api/04-checkout.md) | `feature/basket-04-checkout` | basket-api/02, ordering-api/02 | Not started |
