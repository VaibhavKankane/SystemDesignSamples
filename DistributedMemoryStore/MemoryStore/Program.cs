using MemoryStore.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace MemoryStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureBuilder(builder);

            var app = builder.Build();
            ConfigureApplication(app);

            app.Run();
        }

        private static void ConfigureApplication(WebApplication app)
        {
            // Configure the HTTP request pipeline.
            app.MapGrpcService<MemoryStoreService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        }

        private static void ConfigureBuilder(WebApplicationBuilder builder)
        {
            // Grpc requires Http/2 with Asp.netcore
            // Prometheus requires Http/1.1 on metrics endpoint
            // ==> Kestrel to listen to 2 endpoints
            // ==> Some links on web show how to configure multiple protocols in the appsettings.json
            //     but it didnt work considering local dev + docker config
            // https://www.mytechramblings.com/posts/some-gotchas-when-deploying-a-dotnet-grpc-app-to-ecs/
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Ensure that docker compose also maps to following ports
                options.Listen(IPAddress.Any, 8080, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
                options.Listen(IPAddress.Any, 8081, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            // Add services to the container.
            builder.Services.AddSingleton<IMemoryStore, MemoryStoreDictImpl>();
            builder.Services.AddGrpc();
        }
    }
}