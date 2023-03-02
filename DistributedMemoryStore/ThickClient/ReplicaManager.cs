using Grpc.Net.Client;
using static MemoryStore.MemoryStore;

namespace ThickClient
{
    public class ReplicaManager : IReplicaManager
    {
        private const string AddressOfLeader = "http://host.docker.internal:8083";
        private MemoryStoreClient _leader;

        public ReplicaManager()
        {
            var channel = GrpcChannel.ForAddress(AddressOfLeader);
            _leader = new(channel);
        }

        public List<MemoryStoreClient> GetAllServingReplicas()
        {
            return new List<MemoryStoreClient> { _leader };
        }

        public MemoryStoreClient GetLeaderReplica()
        {
            return _leader;
        }

        public MemoryStoreClient GetReplicaForRead()
        {
            return _leader;
        }
    }
}
