using static MemoryStore.MemoryStore;

namespace MemoryStore.Common
{
    public interface IReplicaManager
    {
        void UpdateReplicas(List<string> replicaPorts);
        
        bool IsLeader();

        MemoryStoreClient? GetLeaderReplica();

        // Used by client to choose a replica. This can be enhanced to consider traffic distribution
        MemoryStoreClient? GetReplicaForRead();

        List<MemoryStoreClient> GetAllServingReplicas();
    }
}