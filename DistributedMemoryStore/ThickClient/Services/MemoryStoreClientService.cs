using Grpc.Core;
using MemoryStore;
using MemoryStore.Common;
using static MemoryStore.MemoryStore;

namespace ThickClient.Services
{
    public class MemoryStoreClientService : MemoryStoreBase
    {
        private readonly IReplicaConnectionManager _replicaConnectionManager;

        public MemoryStoreClientService(IReplicaConnectionManager replicaConnectionManager)
        {
            _replicaConnectionManager = replicaConnectionManager;
        }

        // Read from any replica
        public override async Task<ReadResponse> Read(ReadRequest request, ServerCallContext context)
        {
            var client = await _replicaConnectionManager.GetReplicaForReadAsync();
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
            var response = client.Read(request);
            return response;
        }

        // Write only to leader
        public override async Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
        {
            // TODO: Retry if error due to leader change in the middle of operation
            var client = await _replicaConnectionManager.GetLeaderReplicaAsync();
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
                var response = client.Write(request);
                return response;
            }
        }

        // Delete only from leader
        public override async Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var client = await _replicaConnectionManager.GetLeaderReplicaAsync();
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
                var response = client.Delete(request);
                return response;
            }
        }
    }
}