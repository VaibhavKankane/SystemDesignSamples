namespace MemoryStore
{
    public class Config
    {
        public readonly string WALFilePath;

        public Config()
        {
            WALFilePath = Environment.GetEnvironmentVariable("WALFilePath");
            if(WALFilePath == null)
            {
                throw new ArgumentNullException("WALFilePath cannot be null");
            }
        }
    }
}
