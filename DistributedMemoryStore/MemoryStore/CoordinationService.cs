using MemoryStore.Common;
using MemoryStore.Common.Zookeeper;

namespace MemoryStore
{
    public class CoordinationService : BackgroundService
    {
        private readonly ZooKeeperClient _zkClient;
        private readonly ServicesWatcher _servicesWatcher;
        private readonly IReplicaManager _replicaManager;
        private readonly Config _config;

        public CoordinationService(ZooKeeperClient zooKeeperClient,
            ServicesWatcher servicesWatcher,
            IReplicaManager replicaManager,
            Config config) 
        {
            _zkClient = zooKeeperClient;
            _servicesWatcher = servicesWatcher;
            _replicaManager = replicaManager;
            _config = config;
        }


        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // configure listeners before zookeeper is connected
            _zkClient.OnSyncConnected += OnConnectAsync;

            // replicaManager will keep the grpc client objects for each services Instance
            _servicesWatcher.OnServiceInstancesUpdated += _replicaManager.UpdateReplicas;
            
            //---
            // TODO: wait for 30 secs before connecting to zookeeper in the start
            // With current code-  it works when services restart and when one goes down and comes back AFTER 30 secs
            // Only scenario pending - when service gets paused/terminated from docker and brought back within 30 secs.
            // In that case, replicas become 0
            //---

            // good to do connect outside ctor, as the object itself is ready now
            _zkClient.Connect();

            return base.StartAsync(cancellationToken);
        }

        private async Task OnConnectAsync()
        {
            // Create persistent node to register service
            await _servicesWatcher.InitAsync();

            // Nominate self for leader of MemStoreService
            await _servicesWatcher.NominateForElectionAsync(_config.HostPort);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait till the task is cancelled
            await stoppingToken.WhenCancelled();
        }
    }

    public static class CancellationTokenExtensions
    {
        public static async Task WhenCancelled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            using (cancellationToken.Register(() => tcs.SetResult(null)))
            {
                await tcs.Task;
            }
        }
    }
}
