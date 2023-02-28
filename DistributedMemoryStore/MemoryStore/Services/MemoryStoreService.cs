using Grpc.Core;
using MemoryStore.RequestQueue;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace MemoryStore.Services
{
    public class MemoryStoreService : MemoryStore.MemoryStoreBase
    {
        private readonly IMemoryStore _memoryStore;
        private readonly IRequestProcessorQueue _requestQueue;

        public MemoryStoreService(IMemoryStore memoryStore, IRequestProcessorQueue requestQueue)
        {
            _memoryStore = memoryStore;
            _requestQueue = requestQueue;
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
            var status = await _requestQueue.ProcessInQueue(request);
            
            var response = new WriteResponse()
            {
                Status = status
            };

            return response;
        }

        public override async Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var status = await _requestQueue.ProcessInQueue(request);

            var response = new DeleteResponse()
            {
                Status = status
            };
            return response;
        }
    }
}
