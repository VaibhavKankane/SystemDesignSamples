using MemoryStore.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
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
            // Enable routing, which is necessary to both:
            // 1) capture metadata about gRPC requests, to add to the labels.
            // 2) expose /metrics in the same pipeline.
            app.UseRouting();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<MemoryStoreService>();
            ConfigurePrometheusMetrics(app);

            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        }

        private static void ConfigurePrometheusMetrics(WebApplication app)
        {
            // Capture metrics about received gRPC requests.
            app.UseGrpcMetrics();

            // Capture metrics about received HTTP requests.
            app.UseHttpMetrics();

            app.UseEndpoints(endpoints =>
            {
                // Enable the /metrics page to export Prometheus metrics.
                // Open http://localhost:8082/metrics to see the metrics.
                //
                // Metrics published in this sample:
                // * built-in process metrics giving basic information about the .NET runtime (enabled by default)
                // * metrics from .NET Event Counters (enabled by default)
                // * metrics from .NET Meters (enabled by default)
                // * metrics about HTTP requests handled by the web app (configured above)
                // * metrics about gRPC requests handled by the web app (configured above)
                endpoints.MapMetrics();
            });
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