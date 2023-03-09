namespace ThickClient
{
    internal class Config
    {
        public string ZookeeperConnection { get; internal set; }

        public Config()
        {
            ZookeeperConnection = Environment.GetEnvironmentVariable("ZK_CONNECTION");
            if (ZookeeperConnection == null)
            {
                throw new ArgumentNullException("ZookeeperConnection cannot be null");
            }
        }
    }
}