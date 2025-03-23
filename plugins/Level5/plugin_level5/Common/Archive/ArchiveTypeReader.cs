using System.Buffers.Binary;
using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    public class ArchiveTypeReader
    {
        public ArchiveType Read(Stream stream)
        {
            if (stream.Length < 4)
                throw new InvalidOperationException("Stream needs to be at least 4 bytes long.");

            var buffer = new byte[4];
            int _ = stream.Read(buffer);

            if (buffer[0] == 'X' && buffer[1] == 'P' && buffer[2] == 'C' && buffer[3] == 'K')
                return ArchiveType.Xpck;

            if (buffer[0] == 'X' && buffer[1] == 'F' && buffer[2] == 'S' && buffer[3] == 'P')
                return ArchiveType.Xfsp;

            throw new InvalidOperationException($"Unknown archive type 0x{BinaryPrimitives.ReadUInt32BigEndian(buffer):X8}");
        }

        public ArchiveType Peek(Stream stream)
        {
            var bkPos = stream.Position;
            var type = Read(stream);

            stream.Position = bkPos;
            return type;
        }

        public bool TryPeek(Stream stream, out ArchiveType archiveType)
        {
            archiveType = default;

            try
            {
                archiveType = Peek(stream);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
