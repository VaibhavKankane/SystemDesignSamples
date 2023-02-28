using Grpc.Net.Client;
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
            await GenerateTraffic(client);
        }

        private static async Task GenerateTraffic(MemoryStoreClient client)
        {
            string baseKey = "key";
            string key = baseKey;
            int i = 0;
            Dictionary<string, string> _dict = new();
            Random rnd = new Random();
            while (true)
            {
                try
                {
                    key = baseKey + i;
                    // write 
                    await Write(client, key);
                    _dict.Add(key, key);

                    // validate
                    var value = await Read(client, key);
                    if(value == _dict[key])
                    {
                        Console.WriteLine("Value returned correctly");
                    }

                    // validate a random key-val
                    var n = rnd.Next(0, _dict.Count);
                    value = await Read(client, _dict.ElementAt(n).Key);
                    if (value == _dict.ElementAt(n).Value)
                    {
                        Console.WriteLine("Value returned correctly");
                    }
                    else 
                    { 
                        Console.WriteLine("Random check failed for -{0}", _dict.ElementAt(n).Key);
                    }
                    i++;

                    //await Task.Delay(500);
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

        private static async Task<string> Read(MemoryStoreClient client, string key)
        {
            // make 3 calls for read
            ReadRequest readRequest = new ReadRequest();
            readRequest.Key = key;
            using (_requestDuration.NewTimer())
            {
                var readResponse = await client.ReadAsync(readRequest);
                if (readResponse.Status.Success)
                {
                    _requestCountByStatus.WithLabels(new[] { "Read", "Success" }).Set(1);
                    return readResponse.Value;
                }
                else
                {
                    _requestCountByStatus.WithLabels(new[] { "Read", readResponse.Status.ErrorCode.ToString() }).Set(1);
                }
            }
            return null;
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