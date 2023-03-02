using static MemoryStore.MemoryStore;

namespace ThickClient
{
    public interface IReplicaManager
    {
        MemoryStoreClient GetLeaderReplica();

        MemoryStoreClient GetReplicaForRead();

        List<MemoryStoreClient> GetAllServingReplicas();
    }
}