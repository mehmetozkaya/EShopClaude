var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var catalogDb = postgres.AddDatabase("catalogdb");
var orderingDb = postgres.AddDatabase("orderingdb");

var basketCache = builder.AddRedis("basket-cache");

var catalogApi = builder.AddProject<Projects.EShopClaude_Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health");

var orderingApi = builder.AddProject<Projects.EShopClaude_Ordering_Api>("ordering-api")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Basket_Api>("basket-api")
    .WithReference(basketCache)
    .WaitFor(basketCache)
    .WithReference(catalogApi)
    .WaitFor(catalogApi)
    .WithReference(orderingApi)
    .WaitFor(orderingApi)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(catalogApi)
    .WaitFor(catalogApi)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
