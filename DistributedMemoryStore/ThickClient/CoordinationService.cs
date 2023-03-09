using MemoryStore.Common;
using MemoryStore.ZooKeeper;

namespace ThickClient
{
    public class CoordinationService : BackgroundService
    {
        private ZooKeeperClient _zkClient;
        private ReplicaManager _replicaManager;

        public CoordinationService(ZooKeeperClient zooKeeperClient,
            ReplicaManager replicaManager) 
        {
            _zkClient = zooKeeperClient;
            _replicaManager = replicaManager;
        }


        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // configure listeners before zookeeper is connected
            _zkClient.OnSyncConnected += OnConnectAsync;

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
            // Initialize the replica manager to start watching for node changes.
            // It will also create the nodes to watch if not created already
            await _replicaManager.InitAsync();
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
