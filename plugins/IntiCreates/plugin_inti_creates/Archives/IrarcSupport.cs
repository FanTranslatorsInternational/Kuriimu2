using System.Buffers.Binary;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class IrarcFileEntry
    {
        public int id;
        public int offset;
        public int size;

        // Bit 4 => IsCompressed
        public int flags;

        public bool IsCompressed => (flags & 0x10) > 0;
    }

    class IrarcArchiveFile : ArchiveFile
    {
        public IrarcFileEntry Entry { get; }

        public IrarcArchiveFile(ArchiveFileInfo fileInfo, IrarcFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }

        public long WriteFileData(Stream output)
        {
            var bkPos = output.Position;

            if (UsesCompression)
                output.Position += 0x18;

            var writtenSize = base.WriteFileData(output, true);

            if (!UsesCompression)
                return writtenSize;

            writtenSize += 0x18;

            (bkPos, output.Position) = (output.Position, bkPos);

            WriteInt32(output, 0x18);
            WriteInt32(output, 0);
            WriteInt32(output, (int)writtenSize);
            WriteInt32(output, (int)FileSize);
            WriteInt32(output, 0);
            WriteInt32(output, 0);

            output.Position = bkPos;

            return writtenSize;
        }

        private void WriteInt32(Stream input, int value)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);

            input.Write(buffer);
        }
    }
}
