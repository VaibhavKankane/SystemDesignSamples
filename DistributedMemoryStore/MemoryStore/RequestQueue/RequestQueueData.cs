namespace MemoryStore.RequestQueue
{
    /// <summary>
    /// This is the data thats passed to pipeline steps
    /// </summary>
    class RequestQueueData
    {
        public WALEntry Entry { get; set; }
        public TaskCompletionSource<ResponseStatus> Status { get; set; }
        public bool FromLeader { get; set; }

        public RequestQueueData()
        {
            Entry = new WALEntry();
            Status = new TaskCompletionSource<ResponseStatus>();
            FromLeader = false;
        }
    }
}
