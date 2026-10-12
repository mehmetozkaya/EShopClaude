var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var catalogDb = postgres.AddDatabase("catalogdb");
var orderingDb = postgres.AddDatabase("orderingdb");

builder.AddProject<Projects.EShopClaude_Catalog_Api>("catalog-api")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Ordering_Api>("ordering-api")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Basket_Api>("basket-api")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
