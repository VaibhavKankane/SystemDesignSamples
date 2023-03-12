using MemoryStore.WAL;
using System.Threading.Tasks.Dataflow;

namespace MemoryStore.RequestQueue
{
    internal class QueueStepAppendToLog
    {
        private readonly LamportClock _clock;
        private readonly IWriteAheadLogger _writeAheadLogger;

        public QueueStepAppendToLog(LamportClock clock, IWriteAheadLogger writeAheadLogger)
        {
            _clock = clock;
            _writeAheadLogger = writeAheadLogger;
        }

        internal TransformBlock<RequestQueueData, RequestQueueData> GetStep()
        {
            return new TransformBlock<RequestQueueData, RequestQueueData>(data =>
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
        }
    }
}