using Grpc.Core;
using MemoryStore;
using MemoryStore.Common;
using static MemoryStore.MemoryStore;

namespace ThickClient.Services
{
    public class MemoryStoreClientService : MemoryStoreBase
    {
        private readonly IReplicaManager _replicaConnectionManager;

        public MemoryStoreClientService(IReplicaManager replicaConnectionManager)
        {
            _replicaConnectionManager = replicaConnectionManager;
        }

        // Read from any replica
        public override async Task<ReadResponse> Read(ReadRequest request, ServerCallContext context)
        {
            var client = _replicaConnectionManager.GetReplicaForRead();
            if(client == null)
            {
                return new ReadResponse()
                {
                    Status = new ResponseStatus()
                    {
                        Success = false,
                        ErrorCode = ErrorCode.ReplicaNotAvailable
                    }
                };
            }
            var response = await client.ReadAsync(request);
            return response;
        }

        // Write only to leader
        public override async Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
        {
            // TODO: Retry if error due to leader change in the middle of operation
            var client = _replicaConnectionManager.GetLeaderReplica();
            if(client == null)
            {
                return new WriteResponse()
                {
                    Status = new ResponseStatus()
                    {
                        Success = false,
                        ErrorCode = ErrorCode.ReplicaNotAvailable
                    }
                };
            }
            else
            {
                var response = await client.WriteAsync(request);
                return response;
            }
        }

        // Delete only from leader
        public override async Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var client = _replicaConnectionManager.GetLeaderReplica();
            if (client == null)
            {
                return new DeleteResponse()
                {
                    Status = new ResponseStatus()
                    {
                        Success = false,
                        ErrorCode = ErrorCode.ReplicaNotAvailable
                    }
                };
            }
            else
            {
                var response = await client.DeleteAsync(request);
                return response;
            }
        }
    }
}