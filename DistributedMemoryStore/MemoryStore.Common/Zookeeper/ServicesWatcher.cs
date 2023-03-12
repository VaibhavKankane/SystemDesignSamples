using Microsoft.Extensions.Logging;
using org.apache.zookeeper;
using static org.apache.zookeeper.ZooDefs;
using System.Text;
using org.apache.zookeeper.data;
using System.IO;
using static org.apache.zookeeper.Watcher.Event;
using System.Collections.Concurrent;

namespace MemoryStore.Common.Zookeeper
{
    public delegate void OnServiceInstancesUpdatesHandler(List<string> serviceInstances);

    /// <summary>
    /// This class serves 2 purposes - service discovery and leader election
    /// The logic for both is same, create ephemeral nodes, so keeping it simple here
    /// </summary>
    public class ServicesWatcher : Watcher
    {
        private ILogger<ServicesWatcher> _logger;
        private ZooKeeperClient _zkClient;
        private readonly object _lockObject = new object();
        private List<string> _services;
        private bool _initialized = false;  // persistent nodes created + watch set
        private string _path = $"{Constants.ServiceRootNodeInZooKeeper}/{Constants.MemoryServiceNodeInZooKeeper}";
        private string _leader; // Even _leader can be read/write by multiple threads,
                                // read from request processing and write from zk events

        public OnServiceInstancesUpdatesHandler OnServiceInstancesUpdated { get; set; }

        public ServicesWatcher(ZooKeeperClient zkClient, ILogger<ServicesWatcher> logger)
        {
            _logger = logger;
            _zkClient = zkClient;
            _services = new();
            _leader = string.Empty;
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
                await _zkClient.CreateAsync(nodeName, Encoding.UTF8.GetBytes("services"), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
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
                // Generally this should be "{path}/n_00000001" 
                // But to simplify in this demo, I am using serviceInstance in name itself
                // Else there would be a separate call to get data for each node to find out port number/address
                // In production scenario, this is even more polished to ensure that the most updated replica is selected as a leader
                // This works well in this demo code and gets the desired functionality for key-value store
                var instanceNode = await _zkClient.CreateAsync($"{_path}/{serviceInstance}n_", Encoding.UTF8.GetBytes(serviceInstance), Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
                _logger.LogError("Nominate for {0}", instanceNode);
            }
        }

        public string GetLeaderServiceInstance()
        {
            // _leader will be set automatically when nodes start nominating for elections.
            // this class is a watcher and will process children everytime for service discovery and leader
            return _leader;
        }

        public override async Task process(WatchedEvent @event)
        {
            _logger.LogInformation("ServicesWatcher: ZK-event: {0}, state: {1}", @event.get_Type(), @event.getState());

            switch (@event.get_Type())
            {
                case EventType.None: // Change in session
                    switch (@event.getState())
                    {
                        case KeeperState.Expired:
                        case KeeperState.Disconnected:
                            lock (_lockObject)
                            {
                                _leader = string.Empty;
                                _services.Clear();
                            }
                            OnServiceInstancesUpdated?.Invoke(_services);
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
                        _logger.LogError("ServiceWatcer: children ==> {0}", child);
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

                // update service instances
                lock (_lockObject)
                {
                    // format children from {port}n_/00001 to {port}
                    // This was the format decided in function - NominateForElectionAsync
                    _services = children.Select(x => x.Substring(0, x.IndexOf("n_"))).ToList();
                    _leader = Encoding.UTF8.GetString(leadChild.Data);
                    _logger.LogError("Leader = {0}", _leader);
                }

                _logger.LogError("Service instances upated - {0}", _services.Count);
            }
            else
            {
                lock (_lockObject)
                {
                    _services.Clear();
                    _leader = string.Empty;
                }
            }

            // Invoke this in all flows where the _services are updated.
            // This includes the scenario of ThickClient when it starts listening, but nodes are already created by then
            // So only init is called and hence this needs to be triggerred.
            // the process() will not be called in ThickClient scenario when the nodes are already created
            OnServiceInstancesUpdated?.Invoke(_services);
        }
    }
}
