using Grpc.Net.Client;
using MemoryStore.Common;
using MemoryStore.ZooKeeper;
using Microsoft.Extensions.Logging;
using static MemoryStore.MemoryStore;

namespace ThickClient
{
    public class ReplicaConnectionManager : IReplicaConnectionManager
    {
        private ReplicaManager _replicaManager;
        private Dictionary<string, MemoryStoreClient> _replicas;
        private ILogger<ReplicaConnectionManager> _logger;
        private readonly object _lockObject = new object();

        public ReplicaConnectionManager(ReplicaManager replicaManager, ILogger<ReplicaConnectionManager> logger)
        {
            _replicaManager = replicaManager;
            _replicas = new Dictionary<string, MemoryStoreClient>();

            _logger = logger;
            _replicas = new();
        }

        public List<MemoryStoreClient> GetAllServingReplicas()
        {
            return _replicas.Select(x => x.Value).ToList();
        }

        public async Task<MemoryStoreClient?> GetLeaderReplicaAsync()
        {
            var leader = _replicaManager.GetLeaderReplica();
            if(string.IsNullOrEmpty(leader))
            {
                return null;
            }

            if(_replicas.ContainsKey(leader))
                return _replicas[leader];
            else
            {
                var channel = GrpcChannel.ForAddress(Constants.BaseAddressOfReplicaWithoutPort + leader);
                MemoryStoreClient client = new MemoryStoreClient(channel);
                _replicas.Add(leader, client);
            }

            return _replicas.FirstOrDefault().Value;
        }

        public async Task<MemoryStoreClient?> GetReplicaForReadAsync()
        {
            if (_replicas.Count == 0)
            {
                _logger.LogError("NO: Replicas not updated yet");
                return null;
            }

            return await GetLeaderReplicaAsync();
        }
    }
}
