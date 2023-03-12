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
        private readonly TransformBlock<RequestQueueData, RequestQueueData> _requestQueue;
        private readonly QueueStepAppendToLog _stepAppendToLog;
        private readonly QueueStepInsertInStore _stepInsertInStore;

        public RequestProcessorQueue(QueueStepAppendToLog stepAppendToLog,
            QueueStepInsertInStore stepInsertInStore)
        {
            _stepAppendToLog = stepAppendToLog;
            _stepInsertInStore = stepInsertInStore;
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
            var step1 = _stepAppendToLog.GetStep();

            // Insert in store
            var step2 = _stepInsertInStore.GetStep();

            // replicate
            // TODO

            var setResult = QueueStepSetResult.GetStep();

            // create the pipeline, link next step only if no error yet
            var options = new DataflowLinkOptions() { PropagateCompletion = true };
            step1.LinkTo(step2, options);
            step2.LinkTo(setResult, options);
            
            //appendToWAL.LinkTo(insertInStore, x => !x.Status.Task.IsCompleted);
            // Not using predicate style here to link the steps
            // In that, its important to handle all cases so that req is not left in the queue
            // If for any case, pipeline is not sure how to pass ahead- it will hang
            // Instad, the last step has ActionBlock that handles the case if the task is already completed
            return step1;
        }
    }
}
