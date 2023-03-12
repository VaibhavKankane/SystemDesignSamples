using System.Threading.Tasks.Dataflow;

namespace MemoryStore.RequestQueue
{
    internal class QueueStepInsertInStore
    {
        private readonly IMemoryStore _memoryStore;

        public QueueStepInsertInStore(IMemoryStore memoryStore)
        {
            _memoryStore = memoryStore;
        }

        internal TransformBlock<RequestQueueData, RequestQueueData> GetStep()
        {
            return new TransformBlock<RequestQueueData, RequestQueueData>(data =>
            {
                MemoryStoreOperationResult result;
                ErrorCode errorCode = ErrorCode.Unknown;
                switch (data.Entry.OperaionType)
                {
                    case OperationType.Insert:
                        result = _memoryStore.Add(data.Entry.Key, data.Entry.Value);
                        if (result == MemoryStoreOperationResult.Failed_AlreadyExist)
                            errorCode = ErrorCode.KeyExists;
                        break;

                    case OperationType.Delete:
                        result = _memoryStore.Delete(data.Entry.Key);
                        if (result == MemoryStoreOperationResult.Failed_NotExist)
                            errorCode = ErrorCode.NotFound;
                        break;

                    default:
                        result = MemoryStoreOperationResult.Failed_NotExist;
                        break;
                }

                if (result != MemoryStoreOperationResult.Success)
                {
                    data.Status.SetResult(new ResponseStatus()
                    {
                        Success = false,
                        ErrorCode = errorCode
                    });
                }
                return data;

            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1
            });
        }
    }
}