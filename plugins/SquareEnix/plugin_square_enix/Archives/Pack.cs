using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_square_enix.Archives
{
    class Pack
    {
        private static readonly int HeaderSize = 0x18;

        private ByteOrder _byteOrder;
        private PackHeader _header;
        private IList<long> _unknownValues;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Decide ByteOrder
            input.Position = 0xA;
            _byteOrder = (ByteOrder)br.ReadInt16();
            br.ByteOrder = _byteOrder;

            // Read header
            input.Position = 0;
            _header = ReadHeader(br);

            // Detect unsupported packs with differencing length information
            if (input.Length != _header.size)
                throw new InvalidOperationException("This PACK is not supported.");

            // Read offsets
            var offsets = ReadIntegers(br, _header.fileCount);

            // Read unknown longs
            _unknownValues = ReadLongs(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var offset in offsets)
            {
                input.Position = offset;
                var entry = ReadEntry(br);

                var subStream = new SubStream(input, offset + entry.fileStart, entry.fileSize);
                input.Position = offset + 8;
                var name = br.ReadNullTerminatedString();

                result.Add(new PackArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, _byteOrder);

            // Calculate offsets
            var sizeOffset = HeaderSize;
            var unknownValueOffset = sizeOffset + files.Count * 4;
            var dataOffset = unknownValueOffset + files.Count * 8;

            // Write files
            var offsets = new List<int>();
            foreach (var file in files.Cast<PackArchiveFile>())
            {
                offsets.Add(dataOffset);

                // Write file name
                output.Position = dataOffset + 8;
                bw.WriteString(file.FilePath.GetName(), Encoding.ASCII);

                // Pad to file start
                var alignment = PackSupport.GetAlignment(file.FilePath.GetExtensionWithDot());
                output.Position = dataOffset + 0x28;
                bw.WriteAlignment(alignment);
                file.Entry.fileStart = (short)(output.Position - dataOffset);

                // Write file data
                output.Position = dataOffset + file.Entry.fileStart;
                var writtenSize = file.WriteFileData(output, true);
                bw.WriteAlignment(4);
                var nextOffset = output.Position;

                // Write file entry
                file.Entry.fileSize = (uint)writtenSize;
                output.Position = dataOffset;
                WriteEntry(file.Entry, bw);

                dataOffset = (int)nextOffset;
            }

            // Write unknown values
            output.Position = unknownValueOffset;
            WriteLongs(_unknownValues, bw);

            // Write offsets
            output.Position = sizeOffset;
            WriteIntegers(offsets, bw);

            // Write header
            _header.fileCount = (short)files.Count;
            _header.size = (int)output.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private PackHeader ReadHeader(BinaryReaderX reader)
        {
            return new PackHeader
            {
                magic = reader.ReadString(4),
                size = reader.ReadInt32(),
                unk1 = reader.ReadInt16(),
                byteOrder = reader.ReadUInt16(),
                fileCount = reader.ReadInt16(),
                headerSize = reader.ReadInt16(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private long[] ReadLongs(BinaryReaderX reader, int count)
        {
            var result = new long[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt64();

            return result;
        }

        private FileEntry ReadEntry(BinaryReaderX reader)
        {
            return new FileEntry
            {
                fileStart = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                fileSize = reader.ReadUInt32()
            };
        }

        private void WriteHeader(PackHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.size);
            writer.Write(header.unk1);
            writer.Write(header.byteOrder);
            writer.Write(header.fileCount);
            writer.Write(header.headerSize);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteLongs(IList<long> entries, BinaryWriterX writer)
        {
            foreach (long entry in entries)
                writer.Write(entry);
        }

        private void WriteEntry(FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileStart);
            writer.Write(entry.unk2);
            writer.Write(entry.fileSize);
        }
    }
}
