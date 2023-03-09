using org.apache.zookeeper.data;
using org.apache.zookeeper;
using ZooKeeperLibrary = org.apache.zookeeper;
using static org.apache.zookeeper.Watcher.Event;
using Microsoft.Extensions.Logging;
using static org.apache.zookeeper.KeeperException;

namespace MemoryStore.ZooKeeper
{

    public delegate Task OnSyncConnectedHandler();

    /// <summary>
    /// Decorator over ZooKeeper class provided from the nuget
    /// This will simplify library specific handling and the retry logic can be in-built
    /// </summary>
    public class ZooKeeperClient : Watcher
    {
        private const int _sessionTimeout = 30000;
        private string _connectionString;
        private ILogger<ZooKeeperClient> _logger;
        private ZooKeeperLibrary.ZooKeeper _zk;

        public OnSyncConnectedHandler OnSyncConnected;

        public ZooKeeperClient(string connectionstring, ILogger<ZooKeeperClient> logger)
        {
            _connectionString = connectionstring;
            _logger = logger;
        }

        public void Connect()
        {
            // Dont invoke from constructor, as 'this' object wont be ready
            _zk = new ZooKeeperLibrary.ZooKeeper(_connectionString, _sessionTimeout, this);
        }

        public Task<Stat> ExistsAsync(string path)
        {
            return _zk.existsAsync(path);
        }

        public async Task<string> CreateAsync(string path, byte[] data, List<ACL> acl, CreateMode createMode)
        {
            try
            {
                return await _zk.createAsync(path, data, acl, createMode);
            }
            catch (NodeExistsException ex)
            {
                // ignore this exception
                return path;
            }
            catch(KeeperException ex)
            {
                _logger.LogError(ex.Message);
            }
            return path;
        }

        public Task<ChildrenResult> GetChildrenAsync(string path, Watcher watcher)
        {
            // dont use the other overload for getChildrenAsync,
            // as it will use the default watcher instance that was used to create zookeeper(_zk) object
            return _zk.getChildrenAsync(path, watcher);
        }

        public Task<DataResult> GetDataAsync(string path)
        {
            return _zk.getDataAsync(path, false);
        }

        public override async Task process(WatchedEvent @event)
        {
            switch (@event.get_Type())
            {
                case Event.EventType.None: // Change in session
                    switch (@event.getState())
                    {
                        case KeeperState.SyncConnected:
                            _logger.LogInformation("ZKConnectionWatcher: ZK-SyncConnected");
                            await OnSyncConnected?.Invoke();
                            break;
                        case KeeperState.Disconnected:
                            _logger.LogInformation("ZKConnectionWatcher: ZK-DisConnected");
                            break;
                        case KeeperState.Expired:
                            _logger.LogError("ZKConnectionWatcher: Session expired. Recreating the client");
                            await _zk.closeAsync(); // All ephemeral nodes will be deleted and watches will be triggered
                            Connect();
                            break;
                        default:
                            _logger.LogWarning("ZKConnectionWatcher: unexpected state- {0}", @event.getState().ToString());
                            break;
                    }
                    break;
                default:
                    _logger.LogInformation("ZKConnectionWatcher: ZK-event:" + @event.get_Type());
                    break;
            }
        }
    }
}
