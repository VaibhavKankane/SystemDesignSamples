using Google.Protobuf;
using System.IO;

namespace MemoryStore.WAL
{
    public class RestoreFromWAL
    {
        private IMemoryStore _memoryStore;
        private string _filePath;

        public RestoreFromWAL(Config config, IMemoryStore memoryStore)
        {
            _memoryStore = memoryStore;
            _filePath = config.WALFilePath;
        }
        public long RestoreMemoryStoreFromLog()
        {
            long lastSequenceNumber = DateTime.UtcNow.Ticks;

            if (!File.Exists(_filePath))
            {
                return lastSequenceNumber;
            }
            else
            {
                using (var fileStream = File.OpenRead(_filePath))
                {
                    WALEntry? entry = null;
                    while (fileStream.Position < fileStream.Length)
                    {
                        try
                        {
                            entry = WALEntry.Parser.ParseDelimitedFrom(fileStream);
                        }
                        catch (InvalidProtocolBufferException e)
                        {
                            // Handle the error
                            Console.WriteLine(e.Message);
                        }

                        if (entry != null)
                        {
                            lastSequenceNumber = entry.SequenceNumber;
                            switch (entry.OperaionType)
                            {
                                case OperationType.Insert:
                                    _memoryStore.Add(entry.Key, entry.Value);
                                    break;
                                case OperationType.Delete:
                                    _memoryStore.Delete(entry.Key);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            return lastSequenceNumber;
        }
    }
}
