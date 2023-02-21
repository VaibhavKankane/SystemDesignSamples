using Grpc.Core;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace MemoryStore.Services
{
    public class MemoryStoreService : MemoryStore.MemoryStoreBase
    {
        private readonly IMemoryStore _memoryStore;

        public MemoryStoreService(IMemoryStore memoryStore)
        {
            _memoryStore = memoryStore;
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

        public override Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
        {
            var status = new ResponseStatus();

            var result = _memoryStore.Add(request.Key, request.Value);

            if (result == MemoryStoreOperationResult.Success)
                status.Success = true;
            else
            {
                status.Success = false;
                status.ErrorCode = ErrorCode.KeyExists;
            }
            
            var response = new WriteResponse()
            {
                Status = status
            };

            return Task.FromResult(response);
        }

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var result = _memoryStore.Delete(request.Key);
            var status = new ResponseStatus();

            if (result == MemoryStoreOperationResult.Success)
                status.Success = true;
            else
            {
                status.Success = false;
                status.ErrorCode = ErrorCode.NotFound;
            }

            var response = new DeleteResponse()
            {
                Status = status
            };
            return Task.FromResult(response);
        }
    }
}
