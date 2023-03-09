using static MemoryStore.MemoryStore;

namespace ThickClient
{
    public interface IReplicaConnectionManager
    {
        Task<MemoryStoreClient?> GetLeaderReplicaAsync();

        Task<MemoryStoreClient?> GetReplicaForReadAsync();

        //Task<List<MemoryStoreClient>?> GetAllServingReplicas();
    }
}