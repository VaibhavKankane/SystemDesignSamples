using MemoryStore.Common;
using org.apache.zookeeper;
using static org.apache.zookeeper.ZooDefs;
using System.Text;

namespace MemoryStore.ZooKeeper
{
    public class ServiceNodeWriter : INodeWriter
    {
        private ILogger<ServiceNodeWriter> _logger;
        private ZooKeeperClient _zkClient;
        private string _host;

        public ServiceNodeWriter(ZooKeeperClient zooKeeperClient, ILogger<ServiceNodeWriter> logger, Config config)
        {
            _logger = logger;
            _zkClient = zooKeeperClient;
            _host = config.HostPort;
        }

        public async void WriteAsync()
        {
            // ensure root/parent node is created
            await CreatePersistentNode();

            // create child node, trigger watch and update children.
            // If children added before watch, there could be a race condition for getting updated children
            await CreateEphemeralNode();
        }

        private async Task CreatePersistentNode()
        {
            // no need to set watch on this node itself; only need to watch children
            var exists = await _zkClient.ExistsAsync(Constants.ServiceRootNodeInZooKeeper);

            if (exists == null)
            {
                await _zkClient.CreateAsync(Constants.ServiceRootNodeInZooKeeper, Encoding.UTF8.GetBytes("Replicas"), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                _logger.LogInformation("Persistent node created");
            }
            else
            {
                _logger.LogInformation("Persistent Path already exist");
            }
        }

        private async Task CreateEphemeralNode()
        {
            // register this service, also triggers the watch
            string serviceInstanceNode = Constants.ServiceRootNodeInZooKeeper + "/" + _host;
            var path = await _zkClient.CreateAsync(serviceInstanceNode, Encoding.UTF8.GetBytes("Idle"), Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL);
            _logger.LogWarning("Service registered on host: {0}, -- {1}", _host, path);
        }
    }
}
