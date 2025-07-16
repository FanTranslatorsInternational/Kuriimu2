using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_sony.Archives.PSARC
{
    class PSARC
    {
        private static readonly int HeaderSize = 0x20;

        private int BlockLength = 1;
        private List<int> CompressedBlockSizes = new();

        public const ushort ZLibHeader = 0x78DA;
        //public const ushort LzmaHeader = 0x????;

        public const ushort AllStarsEncryptionA = 0x0001;
        public const ushort AllStarsEncryptionB = 0x0002;

        private PsarcHeader _header;
        public bool AllStarsEncryptedArchive;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = ReadHeader(br);

            // Read file entries
            var fileEntries = ReadEntries(br, _header.TocEntryCount);

            // Read block sizes
            var blockCount = (_header.TocSize - (int)br.BaseStream.Position) / 2;
            var blockSizes = ReadBlockSizes(br, blockCount);
            var blockInfos = new List<(int, int)>();

            // Create block infos
            var blockOffset = _header.TocSize;
            foreach (var blockSize in blockSizes.Select(x => x == 0 ? _header.BlockSize : x))
            {
                blockInfos.Add((blockOffset, blockSize));
                blockOffset += blockSize;
            }

            // Check for SDAT Encryption
            input.Position = fileEntries[0].SizeInfo.Offset;
            var compression = br.ReadInt16();
            AllStarsEncryptedArchive = compression == AllStarsEncryptionA || compression == AllStarsEncryptionB;

            // Read file names
            IList<string> fileNames;
            if (AllStarsEncryptedArchive)
            {
                // Temporary until we can decrypt AllStars PSARCs.
                fileNames = Enumerable.Range(1, _header.TocEntryCount - 1).Select(x => $"{x:00000000}.bin").ToArray();
            }
            else
            {
                var manifestStream = new PsarcStream(input, _header.BlockSize, fileEntries[0], blockInfos);
                using var brNames = new BinaryReaderX(manifestStream, Encoding.UTF8);

                fileNames = new List<string>();
                for (var i = 1; i < _header.TocEntryCount; i++)
                    fileNames.Add(ReadName(brNames));
            }

            // Add Files
            var result = new List<IArchiveFile>();
            for (var i = 1; i < fileEntries.Length; i++)
            {
                var fileStream = new PsarcStream(input, _header.BlockSize, fileEntries[i], blockInfos);
                var fileName = fileNames[i - 1];

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }));
            }

            return result;
        }

        private PsarcHeader ReadHeader(BinaryReaderX reader)
        {
            return new PsarcHeader
            {
                Magic = reader.ReadString(4),
                Major = reader.ReadUInt16(),
                Minor = reader.ReadUInt16(),
                Compression = reader.ReadString(4),
                TocSize = reader.ReadInt32(),
                TocEntrySize = reader.ReadInt32(),
                TocEntryCount = reader.ReadInt32(),
                BlockSize = reader.ReadInt32(),
                ArchiveFlags = (ArchiveFlags)reader.ReadInt32()
            };
        }

        private PsarcEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PsarcEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PsarcEntry ReadEntry(BinaryReaderX reader)
        {
            return new PsarcEntry
            {
                MD5Hash = reader.ReadBytes(0x10),
                FirstBlockIndex = reader.ReadInt32(),
                SizeInfo = BinaryTypeReader.Read<PsarcSizeInfo>(reader)!
            };
        }

        private ushort[] ReadBlockSizes(BinaryReaderX reader, int count)
        {
            var result = new ushort[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt16();

            return result;
        }

        private string ReadName(BinaryReaderX reader)
        {
            var bytes = new List<byte>();

            byte value;
            while (reader.BaseStream.Position < reader.BaseStream.Length
                   && (value = reader.ReadByte()) is not 0 and not 10)
                bytes.Add(value);

            return Encoding.UTF8.GetString([.. bytes]);
        }
    }
}
