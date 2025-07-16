using System.Text;
using Komponent.IO;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.Archive.Models;
using plugin_level5.Common.Compression;

namespace plugin_level5.Common.Archive
{
    internal class XfspWriter : IArchiveWriter
    {
        private const int HeaderSize_ = 0x14;
        private const int EntrySize_ = 0xC;

        private readonly Compressor _compressor = new();
        private readonly Crc16 _crc16 = Crc16.X25;

        public void Write(ArchiveData archiveData, Stream output)
        {
            using var bw = new BinaryWriterX(output, true);

            // Write strings
            IDictionary<string, long> stringOffsets = CacheStrings(archiveData.Files);
            Stream stringStream = WriteStrings(stringOffsets, archiveData.StringCompression);

            long stringOffset = HeaderSize_ + archiveData.Files.Count * EntrySize_;

            output.Position = stringOffset;
            stringStream.CopyTo(output);

            // Write files
            long dataOffset = (stringOffset + stringStream.Length + 3) & ~3;

            IList<XfspEntry> fileEntries = WriteFiles(archiveData.Files, output, dataOffset, stringOffsets);

            // Write entries
            output.Position = HeaderSize_;
            WriteEntries(fileEntries, bw);

            // Write header
            XfspHeader header = CreateHeader(fileEntries, archiveData.ContentType, dataOffset, dataOffset - stringOffset, output.Length - dataOffset);

            output.Position = 0;
            WriteHeader(header, bw);

            output.Position = 0;
        }

        private IDictionary<string, long> CacheStrings(IList<ArchiveNamedEntry> entries)
        {
            var result = new Dictionary<string, long>();

            var offset = 0;
            foreach (ArchiveNamedEntry entry in entries)
            {
                if (!result.TryAdd(entry.Name, offset))
                    continue;

                offset += Encoding.ASCII.GetByteCount(entry.Name) + 1;
            }

            return result;
        }

        private Stream WriteStrings(IDictionary<string, long> stringOffsets, Level5CompressionMethod stringCompression)
        {
            var result = new MemoryStream();

            using var bw = new BinaryWriterX(result, false);

            foreach (string key in stringOffsets.Keys)
            {
                long offset = stringOffsets[key];

                result.Position = offset;
                bw.WriteString(key, Encoding.ASCII);
            }

            result.Position = 0;
            Stream compressedResult = _compressor.Compress(result, stringCompression);

            return compressedResult;
        }

        private IList<XfspEntry> WriteFiles(IList<ArchiveNamedEntry> entries, Stream output, long dataOffset, IDictionary<string, long> stringOffsets)
        {
            var result = new XfspEntry[entries.Count];

            long localDataOffset = dataOffset;
            for (var i = 0; i < entries.Count; i++)
            {
                result[i] = new XfspEntry
                {
                    hash = _crc16.ComputeValue(entries[i].Name),
                    nameOffset = (ushort)stringOffsets[entries[i].Name],

                    fileOffsetUpper = (byte)((localDataOffset - dataOffset) >> 18),
                    fileOffsetLower = (ushort)((localDataOffset - dataOffset) >> 2),

                    fileSizeUpper = (byte)(entries[i].Content.Length >> 16),
                    fileSizeLower = (ushort)entries[i].Content.Length
                };

                output.Position = localDataOffset;

                entries[i].Content.Position = 0;
                entries[i].Content.CopyTo(output);

                // TODO: Recompress RES.bin

                long lengthRemainder = entries[i].Content.Length % 4;
                if (lengthRemainder > 0)
                {
                    output.Write(new byte[lengthRemainder]);
                    localDataOffset += entries[i].Content.Length + lengthRemainder;
                }
                else
                {
                    output.Write(new byte[4]);
                    localDataOffset += entries[i].Content.Length + 4;
                }
            }

            return result;
        }

        private void WriteEntries(IList<XfspEntry> entries, BinaryWriterX writer)
        {
            foreach (XfspEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(XfspEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.nameOffset);

            writer.Write(entry.fileOffsetLower);
            writer.Write(entry.fileSizeLower);

            writer.Write(entry.fileOffsetUpper);
            writer.Write(entry.fileSizeUpper);
        }

        private XfspHeader CreateHeader(IList<XfspEntry> entries, byte type, long dataOffset, long nameSize, long dataSize)
        {
            return new XfspHeader
            {
                magic = "XFSP",
                fileCountAndType = (ushort)((entries.Count & 0xFFF) | (type << 0xC)),

                infoOffset = HeaderSize_ >> 2,
                nameTableOffset = (ushort)((HeaderSize_ + entries.Count * EntrySize_) >> 2),
                dataOffset = (ushort)(dataOffset >> 2),

                infoSize = (ushort)((entries.Count * EntrySize_) >> 2),
                nameTableSize = (ushort)(nameSize >> 2),
                dataSize = (ushort)(dataSize >> 2)
            };
        }

        private void WriteHeader(XfspHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, Encoding.ASCII, false, false);
            writer.Write(header.fileCountAndType);

            writer.Write(header.infoOffset);
            writer.Write(header.nameTableOffset);
            writer.Write(header.dataOffset);

            writer.Write(header.infoSize);
            writer.Write(header.nameTableSize);
            writer.Write(header.dataSize);
        }
    }
}
