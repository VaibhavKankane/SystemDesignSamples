using MemoryStore.WAL;
using System.Threading.Tasks.Dataflow;

namespace MemoryStore.RequestQueue
{
    public interface IRequestProcessorQueue
    {
        Task<ResponseStatus> ProcessInQueue(WriteRequest request);

        Task<ResponseStatus> ProcessInQueue(DeleteRequest request);
    }

    class RequestProcessorQueue : IRequestProcessorQueue
    {
        private readonly LamportClock _clock;
        private readonly IWriteAheadLogger _writeAheadLogger;
        private readonly IMemoryStore _memoryStore;
        private readonly TransformBlock<RequestQueueData, RequestQueueData> _requestQueue;

        public RequestProcessorQueue(LamportClock clock, IWriteAheadLogger writeAheadLogger, IMemoryStore memoryStore)
        {
            _clock = clock;
            _writeAheadLogger = writeAheadLogger;
            _memoryStore = memoryStore;
            _requestQueue = CreateRequestQueue();
        }

        public async Task<ResponseStatus> ProcessInQueue(WriteRequest request)
        {
            var reqData = new RequestQueueData();
            reqData.Entry.Key = request.Key;
            reqData.Entry.Value = request.Value;
            reqData.Entry.OperaionType = OperationType.Insert;

            await _requestQueue.SendAsync(reqData);
            var tcs = await reqData.Status.Task;
            return tcs;
        }

        public async Task<ResponseStatus> ProcessInQueue(DeleteRequest request)
        {
            var reqData = new RequestQueueData();
            reqData.Entry.Key = request.Key;
            reqData.Entry.OperaionType = OperationType.Delete;

            await _requestQueue.SendAsync(reqData);
            var tcs = await reqData.Status.Task;
            return tcs;
        }

        private TransformBlock<RequestQueueData, RequestQueueData> CreateRequestQueue()
        {
            // Add to WAL
            TransformBlock<RequestQueueData, RequestQueueData> appendToWAL = GetAppendToLogFn();

            // Insert in store
            var insertInStore = GetInsertInStoreFn();

            // replicate
            // TODO

            ActionBlock<RequestQueueData> setResult = GetSetResultFn();

            // create the pipeline, link next step only if no error yet
            var options = new DataflowLinkOptions() { PropagateCompletion = true };
            appendToWAL.LinkTo(insertInStore, options);
            insertInStore.LinkTo(setResult, options);
            //appendToWAL.LinkTo(insertInStore, x => !x.Status.Task.IsCompleted);
            //[NOT Working] insertInStore.LinkTo(setResult, options);  // TODO: The predicate seems to not work consistently here.
            // Once its set to false, it sort of always remains false
            // and the setResult Action never gets called it it hangs.
            // So not using predicate here, handling the TCS in the setResult Action itself

            return appendToWAL;
        }

        private static ActionBlock<RequestQueueData> GetSetResultFn()
        {
            return new ActionBlock<RequestQueueData>(data =>
            {
                if (data.Status.Task.IsCompleted)
                {
                    return;
                }
                else
                {
                    data.Status.SetResult(new ResponseStatus()
                    {
                        Success = true
                    });
                }
            });
        }

        private TransformBlock<RequestQueueData, RequestQueueData> GetAppendToLogFn()
        {
            var appendToWAL = new TransformBlock<RequestQueueData, RequestQueueData>(data =>
            {
                try
                {
                    // generate and assign sequence number
                    data.Entry.SequenceNumber = _clock.GetNext();

                    _writeAheadLogger.AppendLog(data.Entry);
                }
                catch (Exception)
                {
                    data.Status.SetResult(new ResponseStatus()
                    {
                        Success = false,
                        ErrorCode = ErrorCode.Unknown
                    });
                }
                return data;
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1
            });
            return appendToWAL;
        }

        private TransformBlock<RequestQueueData, RequestQueueData> GetInsertInStoreFn()
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
