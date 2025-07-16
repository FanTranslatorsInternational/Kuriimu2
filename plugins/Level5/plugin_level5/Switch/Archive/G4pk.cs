using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum.Crc;

namespace plugin_level5.Switch.Archive
{
    // Game: Yo-kai Watch 4
    public class G4pk
    {
        private const int HeaderSize_ = 64;
        private const int OffsetSize_ = 4;
        private const int LengthSize_ = 4;
        private const int HashSize_ = 4;
        private const int UnkIdsSize_ = 2;
        private const int StringOffsetSize_ = 2;

        private G4pkHeader _header;
        private IList<short> _unkIds;

        public List<ArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Header
            _header = ReadHeader(br);

            // Entry information
            br.BaseStream.Position = _header.headerSize;
            var fileOffsets = ReadIntegers(br, _header.fileCount);
            var fileSizes = ReadIntegers(br, _header.fileCount);
            var hashes = ReadUnsignedIntegers(br, _header.table2EntryCount);

            // Unknown information
            _unkIds = ReadShorts(br, _header.table3EntryCount / 2);

            // Strings
            br.BaseStream.Position = (br.BaseStream.Position + 3) & ~3;
            var stringOffset = br.BaseStream.Position;
            var stringOffsets = ReadShorts(br, _header.table3EntryCount / 2);

            //Files
            var result = new List<ArchiveFile>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                br.BaseStream.Position = stringOffset + stringOffsets[i];
                var name = br.ReadNullTerminatedString();

                var fileStream = new SubStream(input, _header.headerSize + (fileOffsets[i] << 2), fileSizes[i]);
                var fileInfo = new ArchiveFileInfo
                {
                    FileData = fileStream,
                    FilePath = name
                };

                result.Add(new ArchiveFile(fileInfo));
            }

            return result;
        }

        public void Save(Stream output, List<ArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            var fileOffsetsPosition = HeaderSize_;
            var fileHashesPosition = fileOffsetsPosition + files.Count * (OffsetSize_ + LengthSize_);
            var unkIdsPosition = fileHashesPosition + files.Count * HashSize_;
            var stringOffsetPosition = (unkIdsPosition + UnkIdsSize_ + 3) & ~3;
            var stringPosition = (stringOffsetPosition + StringOffsetSize_ + 3) & ~3;

            // Write strings
            var crc32 = Crc32.Crc32B;

            bw.BaseStream.Position = stringOffsetPosition;
            var fileHashes = new List<uint>();
            var relativeStringOffset = stringPosition - stringOffsetPosition;
            for (var i = 0; i < files.Count; i++)
            {
                // Write string offset
                bw.BaseStream.Position = stringOffsetPosition + i * StringOffsetSize_;
                bw.Write((short)relativeStringOffset);

                // Add hash
                fileHashes.Add(crc32.ComputeValue(files[i].FilePath.ToRelative().FullName));

                // Write string
                bw.BaseStream.Position = stringOffsetPosition + relativeStringOffset;
                bw.WriteString(files[i].FilePath.ToRelative().FullName);

                relativeStringOffset = (int)(bw.BaseStream.Position - stringOffsetPosition);
            }

            var fileDataPosition = (bw.BaseStream.Position + 15) & ~15;

            // Write file data
            bw.BaseStream.Position = fileDataPosition;

            var fileOffsets = new List<int>();
            var fileSizes = new List<int>();
            foreach (var file in files)
            {
                fileOffsets.Add((int)((bw.BaseStream.Position - HeaderSize_) >> 2));

                var writtenSize = file.WriteFileData(bw.BaseStream, false);
                bw.WriteAlignment(0x20);

                fileSizes.Add((int)writtenSize);
            }

            // Write file information
            bw.BaseStream.Position = fileOffsetsPosition;

            WriteIntegers(fileOffsets, bw);
            WriteIntegers(fileSizes, bw);
            WriteUnsignedIntegers(fileHashes, bw);

            // Write unknown information
            WriteShorts(_unkIds, bw);

            // Write header
            bw.BaseStream.Position = 0;

            _header.fileCount = files.Count;
            _header.contentSize = (int)(bw.BaseStream.Length - HeaderSize_);
            _header.table2EntryCount = (short)fileHashes.Count;

            WriteHeader(_header, bw);
        }

        private G4pkHeader ReadHeader(BinaryReaderX reader)
        {
            return new G4pkHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt16(),
                fileType = reader.ReadInt16(),
                version = reader.ReadInt32(),
                contentSize = reader.ReadInt32(),
                zeroes1 = reader.ReadBytes(0x10),
                fileCount = reader.ReadInt32(),
                table2EntryCount = reader.ReadInt16(),
                table3EntryCount = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                unk3 = reader.ReadInt16(),
                zeroes2 = reader.ReadBytes(0x14)
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private uint[] ReadUnsignedIntegers(BinaryReaderX reader, int count)
        {
            var result = new uint[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        private short[] ReadShorts(BinaryReaderX reader, int count)
        {
            var result = new short[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt16();

            return result;
        }

        private void WriteHeader(G4pkHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.fileType);
            writer.Write(header.version);
            writer.Write(header.contentSize);
            writer.Write(header.zeroes1);
            writer.Write(header.fileCount);
            writer.Write(header.table2EntryCount);
            writer.Write(header.table3EntryCount);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.zeroes2);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteUnsignedIntegers(IList<uint> entries, BinaryWriterX writer)
        {
            foreach (uint entry in entries)
                writer.Write(entry);
        }

        private void WriteShorts(IList<short> entries, BinaryWriterX writer)
        {
            foreach (short entry in entries)
                writer.Write(entry);
        }
    }
}
