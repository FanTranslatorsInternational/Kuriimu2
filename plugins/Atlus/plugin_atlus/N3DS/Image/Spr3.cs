using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_atlus.N3DS.Image
{
    class Spr3
    {
        private static readonly Guid CtpkId = Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");

        private const int HeaderSize = 0x20;
        private const int OffsetSize = 0x8;

        private Spr3Header _header;

        private IList<byte[]> _entries;
        private IList<IFileState> _ctpkStates;

        public IReadOnlyList<IImageFile> Load(Stream input, IPluginFileManager manager)
        {
            using var br = new BinaryReaderX(input);

            // Read header and offsets
            _header = ReadHeader(br);
            var ctpkOffsets = ReadOffsets(br, _header.imgOffset, _header.imgCount);
            var entryOffsets = ReadOffsets(br, _header.entryOffset, _header.entryCount);

            // Read entries and load CTPKs
            _entries = ReadEntries(br, entryOffsets);
            _ctpkStates = new List<IFileState>();

            var images = new List<IImageFile>();
            for (var i = 0; i < ctpkOffsets.Length; i++)
                images.AddRange(LoadCtpk(input, manager, ctpkOffsets, i));

            return images;
        }

        public void Save(Stream output, IPluginFileManager manager)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            int imgOffset = HeaderSize;
            int entryOffsetsOffset = imgOffset + _ctpkStates.Count * OffsetSize;
            int entryOffset = entryOffsetsOffset + _entries.Count * OffsetSize;
            int dataOffset = (entryOffset + _entries.Sum(x => x.Length) + 0x3F) & ~0x3F;

            // Write CTPK data and capture their starting positions
            List<int> imgOffsets = WriteCtpkData(output, manager, dataOffset);

            // Write entries and capture their offsets
            List<int> entryDataOffsets = WriteEntries(bw, entryOffset);

            // Write offset tables for entries and CTPKs
            WriteOffsetTable(bw, entryOffsetsOffset, entryDataOffsets);
            WriteOffsetTable(bw, imgOffset, imgOffsets);

            // Write header at the beginning of the stream
            _header.entryOffset = entryOffsetsOffset;
            _header.entryCount = (short)_entries.Count;
            _header.imgOffset = imgOffset;
            _header.imgCount = (short)_ctpkStates.Count;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private Spr3Header ReadHeader(BinaryReaderX reader)
        {
            return new Spr3Header
            {
                const0 = reader.ReadInt32(),
                const1 = reader.ReadInt32(),
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                imgCount = reader.ReadInt16(),
                entryCount = reader.ReadInt16(),
                imgOffset = reader.ReadInt32(),
                entryOffset = reader.ReadInt32()
            };
        }

        private Spr3Offset[] ReadOffsets(BinaryReaderX reader, int offset, short count)
        {
            var result = new Spr3Offset[count];

            reader.BaseStream.Position = offset;
            for (var i = 0; i < count; i++)
                result[i] = ReadOffset(reader);

            return result;
        }

        private Spr3Offset ReadOffset(BinaryReaderX reader)
        {
            return new Spr3Offset
            {
                zero1 = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };
        }

        private void WriteHeader(Spr3Header header, BinaryWriterX writer)
        {
            writer.Write(header.const0);
            writer.Write(header.const1);
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.unk1);
            writer.Write(header.imgCount);
            writer.Write(header.entryCount);
            writer.Write(header.imgOffset);
            writer.Write(header.entryOffset);
        }

        private static IList<byte[]> ReadEntries(BinaryReaderX reader, IEnumerable<Spr3Offset> entryOffsets)
        {
            var entries = new List<byte[]>();
            foreach (var entryOffset in entryOffsets)
            {
                reader.BaseStream.Position = entryOffset.offset;
                entries.Add(reader.ReadBytes(0x80));
            }
            return entries;
        }

        private IReadOnlyList<IImageFile> LoadCtpk(Stream input, IPluginFileManager manager, IList<Spr3Offset> ctpkOffsets, int index)
        {
            var currentOffset = ctpkOffsets[index].offset;
            long nextOffset = (index + 1) < ctpkOffsets.Count ? ctpkOffsets[index + 1].offset : input.Length;
            var length = nextOffset - currentOffset;

            using var ctpkStream = new SubStream(input, currentOffset, length);
            var loadResult = manager.LoadFile(new StreamFile { Stream = ctpkStream, Path = "file.ctpk" }, CtpkId).Result;
            if (loadResult.Status != LoadStatus.Successful)
                throw new InvalidOperationException(loadResult.Status.ToString());

            _ctpkStates.Add(loadResult.LoadedFileState!);
            var pluginState = loadResult.LoadedFileState.PluginState as IImageFilePluginState;
            return pluginState.Images;
        }

        private List<int> WriteCtpkData(Stream output, IPluginFileManager manager, int startingDataOffset)
        {
            var imgOffsets = new List<int>();
            int dataPosition = startingDataOffset;

            foreach (var ctpkState in _ctpkStates)
            {
                Stream ctpkStream = GetCtpkStream(manager, ctpkState);
                output.Position = dataPosition;

                ctpkStream.Position = 0;
                ctpkStream.CopyTo(output);

                imgOffsets.Add(dataPosition);
                dataPosition += (int)ctpkStream.Length;
            }
            return imgOffsets;
        }

        private static Stream GetCtpkStream(IPluginFileManager manager, IFileState ctpkState)
        {
            if (!ctpkState.StateChanged)
            {
                return ctpkState.FileSystem.OpenFile(ctpkState.FilePath);
            }

            var saveResult = manager.SaveStream(ctpkState).Result;
            if (!saveResult.IsSuccessful)
            {
                throw new InvalidOperationException(saveResult.ToString());
            }
            return saveResult.SavedStreams[0].Stream;
        }

        private List<int> WriteEntries(BinaryWriterX bw, int entryStartPosition)
        {
            var entryOffsets = new List<int>();
            bw.BaseStream.Position = entryStartPosition;

            foreach (var entry in _entries)
            {
                entryOffsets.Add((int)bw.BaseStream.Position);
                bw.Write(entry);
            }
            return entryOffsets;
        }

        private static void WriteOffsetTable(BinaryWriterX bw, int tableStartPosition, List<int> offsets)
        {
            bw.BaseStream.Position = tableStartPosition;
            foreach (int offset in offsets)
            {
                bw.Write(0);
                bw.Write(offset);
            }
        }
    }
}
