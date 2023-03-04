namespace MemoryStore
{
    public class Config
    {
        public readonly string WALFilePath;
        public readonly string HostPort;
        public readonly string ZookeeperConnection;
        public readonly string InstanceId;

        public Config()
        {
            WALFilePath = Environment.GetEnvironmentVariable("WALFilePath");
            if (WALFilePath == null)
            {
                throw new ArgumentNullException("WALFilePath cannot be null");
            }

            HostPort = Environment.GetEnvironmentVariable("Host");
            if (HostPort == null)
            {
                throw new ArgumentNullException("HostPort cannot be null");
            }

            ZookeeperConnection = Environment.GetEnvironmentVariable("ZK_CONNECTION");
            if (ZookeeperConnection == null)
            {
                throw new ArgumentNullException("ZookeeperConnection cannot be null");
            }

            InstanceId = Environment.GetEnvironmentVariable("InstanceId");
            if (ZookeeperConnection == null)
            {
                throw new ArgumentNullException("InstanceId cannot be null");
            }
        }
    }
}
