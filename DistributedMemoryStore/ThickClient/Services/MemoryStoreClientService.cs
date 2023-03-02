using Grpc.Core;
using MemoryStore;
using static MemoryStore.MemoryStore;

namespace ThickClient.Services
{
    public class MemoryStoreClientService : MemoryStoreBase
    {
        private readonly IReplicaManager _replicaManager;

        public MemoryStoreClientService(IReplicaManager replicaManager)
        {
            _replicaManager = replicaManager;
        }

        // Read from any replica
        public override Task<ReadResponse> Read(ReadRequest request, ServerCallContext context)
        {
            var client = _replicaManager.GetReplicaForRead();
            var response = client.Read(request);
            return Task.FromResult(response);
        }

        // Write only to leader
        public override Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
        {
            // TODO: Retry if error due to leader change in the middle of operation
            var client = _replicaManager.GetLeaderReplica();
            var response = client.Write(request);
            return Task.FromResult(response);
        }

        // Delete only from leader
        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var client = _replicaManager.GetLeaderReplica();
            var response = client.Delete(request);
            return Task.FromResult(response);
        }
    }
}