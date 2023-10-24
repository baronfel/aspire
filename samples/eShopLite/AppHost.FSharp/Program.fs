open Aspire.Hosting
open Aspire.Hosting.ApplicationModel
open Aspire.Hosting.Redis

[<EntryPoint>]
let main argv =
       let builder = DistributedApplication.CreateBuilder(argv)

       builder.AddAzureProvisioning() |> ignore<_>

       let catalogDb: IDistributedApplicationResourceBuilder<_> = 
              builder.AddPostgresContainer("postgres")
                     .AddDatabase("catalogdb")

       let basketCache: IDistributedApplicationResourceBuilder<RedisContainerResource> = builder.AddRedisContainer("basketcache")

       let catalogService = 
              builder.AddProject<Projects.CatalogService>("catalogservice")
                            .WithReference(catalogDb :?> IDistributedApplicationResourceBuilder<IDistributedApplicationResourceWithConnectionString>)
                            // .WithReplicas(2)

       let ordersQueue = builder.AddAzureServiceBus("messaging", queueNames = [| "orders" |])

       let basketService: IDistributedApplicationResourceBuilder<ProjectResource> = 
              builder.AddProject<Projects.BasketService>("basketservice")
                     .WithReference(basketCache :?> IDistributedApplicationResourceBuilder<IDistributedApplicationResourceWithConnectionString>)
                     .WithReference(ordersQueue :?> IDistributedApplicationResourceBuilder<IDistributedApplicationResourceWithConnectionString>, optional = true)

       builder.AddProject<Projects.MyFrontend>("frontend")
              .WithReference(basketService)
              .WithReference(catalogService.GetEndpoint("http")) |> ignore<_>

       builder.AddProject<Projects.OrderProcessor>("orderprocessor")
              .WithReference(ordersQueue :?> IDistributedApplicationResourceBuilder<IDistributedApplicationResourceWithConnectionString>, optional = true)
              .WithLaunchProfile("OrderProcessor") |> ignore<_>

       builder.AddProject<Projects.ApiGateway>("apigateway")
              .WithReference(basketService)
              .WithReference(catalogService) |> ignore<_>

       builder.AddProject<Projects.CatalogDb>("catalogdbapp")
              .WithReference(catalogDb :?> IDistributedApplicationResourceBuilder<IDistributedApplicationResourceWithConnectionString>) |> ignore<_>

       builder.Build().Run() |> ignore<_>
       0
