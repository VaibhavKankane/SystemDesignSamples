using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MemoryStore.Common;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Threading.Tasks.Dataflow;

namespace MemoryStore.RequestQueue
{
    internal class QueueStepReplicate
    {
        private IReplicaManager _replicaManager;

        public QueueStepReplicate(IReplicaManager replicaManager)
        {
            _replicaManager = replicaManager;
        }


        internal TransformBlock<RequestQueueData, RequestQueueData> GetStep()
        {
            return new TransformBlock<RequestQueueData, RequestQueueData>(async data =>
            {
                // do not replicate if request is from leader
                if (data.FromLeader)
                    return data;

                int successCount = 0;
                Metadata header = new Metadata();
                header.Add(Constants.LeaderHeader, "1");
                var _replicas = _replicaManager.GetAllServingReplicas();

                try
                {
                    switch (data.Entry.OperaionType)
                    {
                        case OperationType.Insert:
                            WriteRequest writeReq = new WriteRequest()
                            {
                                Key = data.Entry.Key,
                                Value = data.Entry.Value
                            };

                            var writeResponses = await Task.WhenAll(_replicas.Select(x => x.WriteAsync(writeReq, header).ResponseAsync));
                            successCount = writeResponses.Where(x => x.Status.Success == true).Count();
                            break;
                        case OperationType.Delete:
                            DeleteRequest deleteReq = new DeleteRequest()
                            {
                                Key = data.Entry.Key
                            };
                            var deleteResponses = await Task.WhenAll(_replicas.Select(x => x.DeleteAsync(deleteReq, header).ResponseAsync));
                            successCount = deleteResponses.Where(x => x.Status.Success == true).Count();
                            break;
                        default:
                            break;
                    }

                    // atleast 1 replica must be there and check if all were successful
                    if (successCount > 0 && successCount == _replicas.Count - 1)
                        return data;
                    else
                    {
                        data.Status.SetResult(new ResponseStatus()
                        {
                            Success = false,
                            ErrorCode = ErrorCode.ReplicationFailed
                        });
                    }
                }
                catch (Exception)
                {
                    data.Status.SetResult(new ResponseStatus()
                    {
                        Success = false,
                        ErrorCode = ErrorCode.ReplicationFailed
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