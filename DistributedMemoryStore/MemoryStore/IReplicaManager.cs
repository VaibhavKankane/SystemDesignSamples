using static MemoryStore.MemoryStore;

namespace MemoryStore
{
    public interface IReplicaManager
    {
        void UpdateReplicas(List<string> replicaPorts);

        MemoryStoreClient? GetLeaderReplica();

        MemoryStoreClient? GetReplicaForRead();

        List<MemoryStoreClient> GetAllServingReplicas();
    }
}