using Grpc.Net.Client;
using MemoryStore.Common;
using MemoryStore.Common.Zookeeper;
using Microsoft.Extensions.Logging;
using static MemoryStore.MemoryStore;

namespace MemoryStore.Common
{
    public class ReplicaManager : IReplicaManager
    {
        private Dictionary<string, MemoryStoreClient> _replicas;
        private readonly ServicesWatcher _servicesWatcher;
        private readonly ILogger<ReplicaManager> _logger;
        private readonly object _lockObject = new object();
        private MemoryStoreClient? _leader;
        private bool _isLeader = false;
        private readonly Random _random;
        private readonly string _currentServingReplica;

        public ReplicaManager(ServicesWatcher servicesWatcher, 
            ILogger<ReplicaManager> logger,
            string currentServingReplica = "")
        {
            _replicas = new();
            _servicesWatcher = servicesWatcher;
            _logger = logger;
            _replicas = new();
            _leader = null;
            _random= new Random();
            _currentServingReplica = currentServingReplica;
        }

        public void UpdateReplicas(List<string> serviceInstances)
        {
            lock (_lockObject)
            {
                // remove stale clients
                _replicas = _replicas.Where(x => serviceInstances.Contains(x.Key)).ToDictionary(y => y.Key, y => y.Value);

                // add clients for new replicas
                foreach (var hostPort in serviceInstances)
                {
                    if (_replicas.ContainsKey(hostPort))
                        continue;
                    else if (hostPort == _currentServingReplica)
                        continue; // Dont create client for calling itself
                    else
                    {
                        var channel = GrpcChannel.ForAddress(Constants.BaseAddressOfReplicaWithoutPort + hostPort);
                        MemoryStoreClient client = new(channel);
                        _replicas.Add(hostPort, client);
                    }
                }

                // set client for leader
                var leaderNode = _servicesWatcher.GetLeaderServiceInstance();
                if(leaderNode == _currentServingReplica)
                {
                    _isLeader = true;
                    _leader = null;
                }
                else if(leaderNode != null && _replicas.Count > 0)
                {
                    // just avoiding a condition when leader is not set and this callback triggered
                    // could happen if all serviceInstances are deleted/not serving
                    _replicas.TryGetValue(leaderNode, out _leader);
                    _isLeader = false;
                }
                else
                {
                    _isLeader = false;
                    _leader = null;
                }
                
            }

            _logger.LogInformation("Replicas upated - {0}", _replicas.Count);
        }

        public bool IsLeader()
        {
            return _isLeader;
        }

        public MemoryStoreClient? GetLeaderReplica()
        {
            return _leader;
        }

        public MemoryStoreClient? GetReplicaForRead()
        {
            lock (_lockObject)
            {
                if (_replicas.Count == 0)
                {
                    _logger.LogError("NO: Replicas not updated yet");
                    return null;
                }

                // Choose a replica at random, could be leader also
                var index = _random.Next(0, _replicas.Count);
                return _replicas.ElementAt(index).Value;
            }
        }

        public List<MemoryStoreClient> GetAllServingReplicas()
        {
            lock(_lockObject)
            {
                return _replicas.Select(x => x.Value).ToList();
            }
        }
    }
}
