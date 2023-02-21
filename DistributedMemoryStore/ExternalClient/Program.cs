﻿using Grpc.Net.Client;
using MemoryStore;
using Prometheus;
using static MemoryStore.MemoryStore;

namespace ExternalClient
{
    internal class Program
    {
        private static Gauge _requestCountByStatus = Metrics.CreateGauge("TotalRequest_ExternalClient",
                "Total number of requests completed from external client.",
                new[] { "RequestType", "Status" });

        private static readonly Gauge _requestDuration = Metrics.CreateGauge("RequestTime_ExternalClient", "Histogram of req durations.");

        static async Task Main(string[] args)
        {
            var assignedPort = Environment.GetEnvironmentVariable("MetricsPort");
            assignedPort ??= "1234";
            var port = Int32.Parse(assignedPort);

            // Start kestrel server for prometheus to scrape
            using var server = new KestrelMetricServer(port);
            server.Start();

            MemoryStoreClient client = GetGrpcClient();

            string key = "key";
            int i = 0;
            Random random = new Random();

            while (true)
            {
                try
                {
                    key = key + i;
                    // write 
                    await Write(client, key);

                    // read n times
                    await Read(client, key);
                    i++;

                    await Task.Delay(1000);
                }
                catch (Grpc.Core.RpcException ex)
                {
                    var labelValues = new List<string>
                    {
                        "Req",
                        ex.Status.StatusCode.ToString()
                    };
                    _requestCountByStatus.WithLabels(labelValues.ToArray()).Inc();

                    continue;
                }

            }
        }

        private static async Task Read(MemoryStoreClient client, string key)
        {
            // make 3 calls for read
            for (int j = 0; j < 3; j++)
            {
                ReadRequest readRequest = new ReadRequest();
                readRequest.Key = key;
                using (_requestDuration.NewTimer())
                {
                    var readResponse = await client.ReadAsync(readRequest);
                    if (readResponse.Status.Success)
                    {
                        _requestCountByStatus.WithLabels(new[] { "Read", "Success" }).Set(1);
                    }
                    else
                    {
                        _requestCountByStatus.WithLabels(new[] { "Read", readResponse.Status.ErrorCode.ToString() }).Set(1);
                    }
                }
            }
        }

        private static async Task Write(MemoryStoreClient client, string key)
        {
            WriteRequest req = new();
            req.Key = key;
            req.Value = req.Key;
            using (_requestDuration.NewTimer())
            {
                var writeResponse = await client.WriteAsync(req);
                if (writeResponse.Status.Success)
                {
                    _requestCountByStatus.WithLabels(new[] { "Write", "Success" }).Set(1);
                }
                else
                {
                    _requestCountByStatus.WithLabels(new[] { "Write", writeResponse.Status.ErrorCode.ToString() }).Set(1);
                }
            }
        }

        private static MemoryStoreClient GetGrpcClient()
        {
            var channel = GrpcChannel.ForAddress("http://host.docker.internal:8083");
            MemoryStoreClient client = new(channel);
            return client;
        }
    }
}