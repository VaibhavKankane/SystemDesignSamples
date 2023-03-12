using System.Threading.Tasks.Dataflow;

namespace MemoryStore.RequestQueue
{
    internal class QueueStepSetResult
    {

        internal static ActionBlock<RequestQueueData> GetStep()
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

    }
}