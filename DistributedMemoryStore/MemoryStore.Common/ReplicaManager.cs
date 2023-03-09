using MemoryStore.Common;
using MemoryStore.ZooKeeper;
using Microsoft.Extensions.Logging;
using org.apache.zookeeper;
using static org.apache.zookeeper.ZooDefs;
using System.Text;
using org.apache.zookeeper.data;
using System.IO;
using static org.apache.zookeeper.Watcher.Event;
using System.Collections.Concurrent;

namespace MemoryStore.Common
{
    public class ReplicaManager : Watcher
    {
        private ILogger<ReplicaManager> _logger;
        private ZooKeeperClient _zkClient;
        private readonly object _lockObject = new object();
        private List<string> _replicas;
        private bool _initialized = false;  // persistent nodes created + watch set
        private string _path = $"{Constants.ServiceRootNodeInZooKeeper}/{Constants.MemoryServiceNodeInZooKeeper}";
        private string _leader; // Even _leader can be read/write by multiple threads,
                                // read from request processing and write from zk events

        public ReplicaManager(ZooKeeperClient zkClient, ILogger<ReplicaManager> logger)
        {
            _logger = logger;
            _zkClient = zkClient;
            _replicas = new();
            _leader= string.Empty;
        }

        public async Task InitAsync()
        {
            /* Create Nodes
             *    /Services[Persistent]
             *         /MemStoreService[Persistent]
            */
            await CreatePersistentNode(Constants.ServiceRootNodeInZooKeeper);
            await CreatePersistentNode(_path);

            _initialized = true;

            // Set watch here
            var result = (await _zkClient.GetChildrenAsync(_path, this)).Children.OrderBy(x => x).ToList();

            await ProcessChildrenAsync(result);
        }

        private async Task CreatePersistentNode(string nodeName)
        {
            var exists = await _zkClient.ExistsAsync(nodeName);
            if (exists == null)
            {
                await _zkClient.CreateAsync(nodeName, Encoding.UTF8.GetBytes("Replicas"), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                _logger.LogInformation("Persistent node {0} created", nodeName);
            }
            else
            {
                _logger.LogInformation("Persistent Path already exist - {0}", nodeName);
            }
        }

        public async Task NominateForElectionAsync(string serviceInstance)
        {
            if (_initialized)
            {
                // Create Ephemeral_Sequential node
                var instanceNode = await _zkClient.CreateAsync($"{_path}/n_", Encoding.UTF8.GetBytes(serviceInstance), Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
                _logger.LogError("Nominate for {0}", instanceNode);
            }
        }

        public string GetLeaderReplica()
        {
            // _leader will be set automatically when nodes start nominating for elections.
            // this class is a watcher and will process children everytime for service discovery and leader
            return _leader;
        }

        public override async Task process(WatchedEvent @event)
        {
            _logger.LogInformation("ReplicaWatcher: ZK-event: {0}, state: {1}", @event.get_Type(), @event.getState());

            switch (@event.get_Type())
            {
                case Event.EventType.None: // Change in session
                    switch (@event.getState())
                    {
                        case KeeperState.Disconnected:
                            _leader = string.Empty;
                            _logger.LogInformation("Clearing out leader ");
                            break;
                    }
                    break;
                
                case EventType.NodeChildrenChanged:
                    // set the watcher to this to keep receiving updates?
                    // Dont set it to 'true' - it means the default watcher,
                    // which is the instance passed while creating zk object
                    var result = await _zkClient.GetChildrenAsync(_path, this);
                    _logger.LogError("Children count = {0}", result.Children.Count);
                    foreach (var child in result.Children)
                    {
                        _logger.LogError("ReplicaManager: children ==> {0}", child);
                    }
                    await ProcessChildrenAsync(result.Children);

                    break;
                case EventType.NodeDeleted:
                    // This could be triggerred when the main persistent node itself is deleted
                    _logger.LogCritical("Main zookeeper node deleted- {0}", _path);
                    break;
                default:
                    break;
            }
        }

        private async Task ProcessChildrenAsync(List<string> children)
        {
            if (children.Count() > 0)
            {
                // Electing first node as the leader, getting its data
                var leadChild = await _zkClient.GetDataAsync($"{_path}/{children.First()}");
                _leader = Encoding.UTF8.GetString(leadChild.Data);

                _logger.LogError("Leader = {0}", _leader);

                // update replicas
                lock (_lockObject)
                {
                    _replicas = children;
                }

                _logger.LogError("Replicas upated - {0}", _replicas.Count);
            }
        }
    }
}
