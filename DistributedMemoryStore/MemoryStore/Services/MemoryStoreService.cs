using Grpc.Core;
using MemoryStore.Common;
using MemoryStore.RequestQueue;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace MemoryStore.Services
{
    public class MemoryStoreService : MemoryStore.MemoryStoreBase
    {
        private readonly IMemoryStore _memoryStore;
        private readonly IRequestProcessorQueue _requestQueue;
        private readonly IReplicaManager _replicaManager;

        public MemoryStoreService(IMemoryStore memoryStore, 
            IRequestProcessorQueue requestQueue,
            IReplicaManager replicaManager)
        {
            _memoryStore = memoryStore;
            _requestQueue = requestQueue;
            _replicaManager = replicaManager;
        }

        public override Task<ReadResponse> Read(ReadRequest request, ServerCallContext context)
        {
            var value = _memoryStore.Get(request.Key);
            var status = new ResponseStatus();

            if (!String.IsNullOrEmpty(value))
                status.Success = true;
            else
            {
                status.Success = false;
                status.ErrorCode = ErrorCode.NotFound;
            }

            var response = new ReadResponse()
            {
                Value = value,
                Status = status
            };
            return Task.FromResult(response);
        }

        public override async Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
        {
            ResponseStatus status;

            bool reqFromLeader = context.RequestHeaders.Get(Constants.LeaderHeader)?.Value == "1";
            bool isLeader = _replicaManager.IsLeader();

            // Check if this is a leader and the request is NOT from a leader
            // else this is not a leader and request IS from leader
            if ((isLeader && !reqFromLeader) || (!isLeader && reqFromLeader))
            {
                status = await _requestQueue.ProcessInQueue(request, reqFromLeader);
            }
            else
            {
                status = new()
                {
                    ErrorCode = ErrorCode.NotLeader,
                    Success = false
                };
            }

            var response = new WriteResponse()
            {
                Status = status
            };

            return response;
        }

        public override async Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            ResponseStatus status;

            bool reqFromLeader = context.RequestHeaders.Get(Constants.LeaderHeader)?.Value == "1";
            bool isLeader = _replicaManager.IsLeader();

            // Check if this is a leader and the request is NOT from a leader
            // else this is not a leader and request IS from leader
            if ((isLeader && !reqFromLeader) || (!isLeader && reqFromLeader))
            {
                status = await _requestQueue.ProcessInQueue(request, reqFromLeader);
            }
            else
            {
                status = new()
                {
                    ErrorCode = ErrorCode.NotLeader,
                    Success = false
                };
            }

            var response = new DeleteResponse()
            {
                Status = status
            };
            return response;
        }
    }
}
