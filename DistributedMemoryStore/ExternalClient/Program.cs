using Grpc.Net.Client;
using MemoryStore;
using static MemoryStore.MemoryStore;

namespace ExternalClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("http://host.docker.internal:8083");
            MemoryStoreClient client = new(channel);

            string key = "key";
            int i = 0;
            Random random = new Random();



            while (true)
            {
                try
                {
                    key = key + i;
                    // write 
                    WriteRequest req = new();
                    req.Key = key;
                    req.Value = req.Key;
                    await client.WriteAsync(req);

                    // read n times
                    int n = random.Next(1, 50);
                    for (int j = 0; j < n; j++)
                    {
                        ReadRequest readRequest = new ReadRequest();
                        readRequest.Key = key;

                        var response = await client.ReadAsync(readRequest);
                        Console.WriteLine(response.Status.ErrorCode.ToString());
                    }
                    i++;

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error - " + ex.Message);
                    continue;
                }
            }

            Console.WriteLine("Hello, World!");
        }
    }
}