using MemoryStore.Common;
using MemoryStore.ZooKeeper;

namespace MemoryStore
{
    public class CoordinationService : BackgroundService
    {
        private ZooKeeperClient _zkClient;
        private INodeWriter _serviceNodeWriter;
        private INodeWatcher _serviceNodeWatcher;

        public CoordinationService(ZooKeeperClient zooKeeperClient,
            INodeWriter serviceNodeWriter,
            INodeWatcher serviceNodeWatcher)
        //IReplicationManager replicationManager) 
        {
            _zkClient = zooKeeperClient;
            _serviceNodeWriter = serviceNodeWriter;
            _serviceNodeWatcher = serviceNodeWatcher;
            //_replicationManager = replicationManager;
        }


        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // configure listeners before zookeeper is connected
            _zkClient.OnSyncConnected += OnConnect;

            //_serviceNodeWatcher.OnNodeChildrenUpdated += _replicationManager.UpdateReplicas;

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

        private void OnConnect()
        {
            // register watch
            _serviceNodeWatcher.RegisterWatchAsync(Constants.ServiceRootNodeInZooKeeper);

            // register self
            _serviceNodeWriter.WriteAsync();
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
