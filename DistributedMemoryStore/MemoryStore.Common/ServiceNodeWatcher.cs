using MemoryStore.Common;
using Microsoft.Extensions.Logging;
using org.apache.zookeeper;
using System.Text;
using static org.apache.zookeeper.Watcher.Event;
using static org.apache.zookeeper.ZooDefs;

namespace MemoryStore.ZooKeeper
{
    public class ServiceNodeWatcher : Watcher, INodeWatcher
    {
        private readonly ILogger<ServiceNodeWatcher> _logger;
        private readonly ZooKeeperClient _zkClient;

        public event OnNodeChildrenUpdatedHandler OnNodeChildrenUpdated;

        public ServiceNodeWatcher(ZooKeeperClient zooKeeperClient, ILogger<ServiceNodeWatcher> logger)
        {
            _logger = logger;
            _zkClient = zooKeeperClient;
        }

        public override async Task process(WatchedEvent @event)
        {
            _logger.LogInformation("ReplicaWatcher: ZK-event: {0}, state: {1}", @event.get_Type(), @event.getState());

            switch (@event.get_Type())
            {
                case EventType.NodeChildrenChanged:
                    // set the watcher to this to keep receiving updates?
                    // Dont set it to 'true' - it means the default watcher,
                    // which is the instance passed while creating zk object
                    var result = await _zkClient.GetChildrenAsync(Constants.ServiceRootNodeInZooKeeper, this);
                    OnNodeChildrenUpdated?.Invoke(result.Children);
                    _logger.LogError("Children count = {0}", result.Children.Count);
                    foreach (var child in result.Children)
                    {
                        _logger.LogError("ReplicaWatcher: children ==> {0}", child);
                    }

                    break;
                case EventType.NodeDeleted:
                    // This could be triggerred when the main persistent node itself is deleted
                    _logger.LogCritical("Main zookeeper node deleted- {0}", Constants.ServiceRootNodeInZooKeeper);
                    break;
                default:
                    break;
            }
        }

        public async Task RegisterWatchAsync(string path)
        {
            // Do store the result here also, else there is a possibility:
            // when service restarts, node is already created, so watch is not triggered
            // and replicas dont get updated. Safer/Correct if this happens twice
            var result = await _zkClient.GetChildrenAsync(path, this);
            OnNodeChildrenUpdated?.Invoke(result.Children);
        }
    }
}
