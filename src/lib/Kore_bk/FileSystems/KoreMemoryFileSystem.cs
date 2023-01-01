using Kontract.Interfaces.Managers.Streams;
using Zio.FileSystems;

namespace Kore.FileSystems
{
    public class KoreMemoryFileSystem : MemoryFileSystem, IKoreFileSystem
    {
        private readonly IStreamManager _streamManager;

        public KoreMemoryFileSystem(IStreamManager streamManager)
        {
            _streamManager = streamManager;
        }

        public KoreMemoryFileSystem(KoreMemoryFileSystem copyFrom, IStreamManager streamManager) : base(copyFrom)
        {
            _streamManager = streamManager;
        }

        public IKoreFileSystem Clone(IStreamManager streamManager)
        {
            EnterFileSystemExclusive();
            try
            {
                return CloneImpl(streamManager);
            }
            finally
            {
                ExitFileSystemExclusive();
            }
            return new KoreMemoryFileSystem(this, streamManager);
        }
    }
}
