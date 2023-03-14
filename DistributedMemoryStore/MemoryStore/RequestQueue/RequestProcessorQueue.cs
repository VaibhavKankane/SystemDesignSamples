using MemoryStore.WAL;
using System.Threading.Tasks.Dataflow;

namespace MemoryStore.RequestQueue
{
    public interface IRequestProcessorQueue
    {
        Task<ResponseStatus> ProcessInQueue(WriteRequest request, bool fromLeader);

        Task<ResponseStatus> ProcessInQueue(DeleteRequest request, bool fromLeader);
    }

    class RequestProcessorQueue : IRequestProcessorQueue
    {
        private readonly TransformBlock<RequestQueueData, RequestQueueData> _requestQueue;
        private readonly QueueStepAppendToLog _stepAppendToLog;
        private readonly QueueStepInsertInStore _stepInsertInStore;
        private readonly QueueStepReplicate _stepReplicate;

        public RequestProcessorQueue(QueueStepAppendToLog stepAppendToLog,
            QueueStepInsertInStore stepInsertInStore,
            QueueStepReplicate stepReplicate)
        {
            _stepAppendToLog = stepAppendToLog;
            _stepInsertInStore = stepInsertInStore;
            _stepReplicate = stepReplicate;
            _requestQueue = CreateRequestQueue();
        }

        public async Task<ResponseStatus> ProcessInQueue(WriteRequest request, bool fromLeader)
        {
            var reqData = new RequestQueueData();
            reqData.Entry.Key = request.Key;
            reqData.Entry.Value = request.Value;
            reqData.Entry.OperaionType = OperationType.Insert;
            reqData.FromLeader = fromLeader;

            await _requestQueue.SendAsync(reqData);
            var tcs = await reqData.Status.Task;
            return tcs;
        }

        public async Task<ResponseStatus> ProcessInQueue(DeleteRequest request, bool fromLeader)
        {
            var reqData = new RequestQueueData();
            reqData.Entry.Key = request.Key;
            reqData.Entry.OperaionType = OperationType.Delete;
            reqData.FromLeader = fromLeader;

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

            // Replicate
            var step3 = _stepReplicate.GetStep();

            // Set result and invoke callback for the request
            var step4 = QueueStepSetResult.GetStep();

            // Create the pipeline, link next step only if no error yet
            var options = new DataflowLinkOptions() { PropagateCompletion = true };
            step1.LinkTo(step2, options);
            step2.LinkTo(step3, options);
            step3.LinkTo(step4, options);
            
            //appendToWAL.LinkTo(insertInStore, x => !x.Status.Task.IsCompleted);
            // Not using predicate style here to link the steps
            // In that, its important to handle all cases so that req is not left in the queue
            // If for any case, pipeline is not sure how to pass ahead- it will hang
            // Instad, the last step has ActionBlock that handles the case if the task is already completed
            return step1;
        }
    }
}
