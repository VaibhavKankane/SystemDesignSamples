using Google.Protobuf;
using System;
using static Grpc.Core.Metadata;

namespace MemoryStore.WAL
{
    public interface IWriteAheadLogger
    {
        void AppendLog(WALEntry entry);
    }

    public class WriteAheadLogger : IWriteAheadLogger
    {
        private readonly string _filePath;
        private readonly FileStream _fileStream;

        public WriteAheadLogger(Config config)
        {
            _filePath = config.WALFilePath;
            _fileStream = File.Open(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public void AppendLog(WALEntry entry)
        {
            entry.WriteDelimitedTo(_fileStream);
            _fileStream.Flush();
        }
    }
}
