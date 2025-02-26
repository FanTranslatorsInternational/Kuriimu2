using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_atlus.N3DS.Image
{
    class Spr3
    {
        private static readonly Guid CtpkId = Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");
        private const int HeaderSize = 32;
        private const int OffsetSize = 8;

        private IList<byte[]> _entries;
        private IList<IFileState> _ctpkStates;

        public IList<ImageFileInfo> Load(Stream input, IPluginFileManager manager)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read header and offsets
            var header = typeReader.Read<Spr3Header>(br);
            var ctpkOffsets = ReadOffsets(input, typeReader, header.imgOffset, header.imgCount);
            var entryOffsets = ReadOffsets(input, typeReader, header.entryOffset, header.entryCount);

            // Read entries and load CTPKs
            _entries = ReadEntries(input, br, entryOffsets);
            _ctpkStates = new List<IFileState>();

            var images = new List<ImageFileInfo>();
            for (int i = 0; i < ctpkOffsets.Count; i++)
            {
                images.AddRange(LoadCtpk(input, manager, ctpkOffsets, i));
            }

            return images;
        }

        public void Save(Stream output, IPluginFileManager manager)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            int imgOffset = HeaderSize;
            int entryOffsetsOffset = imgOffset + _ctpkStates.Count * OffsetSize;
            int entryOffset = entryOffsetsOffset + _entries.Count * OffsetSize;
            int dataOffset = entryOffset + _entries.Sum(x => x.Length);

            // Write CTPK data and capture their starting positions
            List<int> imgOffsets = WriteCtpkData(output, manager, dataOffset);

            // Write entries and capture their offsets
            List<int> entryDataOffsets = WriteEntries(output, bw, entryOffset);

            // Write offset tables for entries and CTPKs
            WriteOffsetTable(output, bw, entryOffsetsOffset, entryDataOffsets);
            WriteOffsetTable(output, bw, imgOffset, imgOffsets);

            // Write header at the beginning of the stream
            var header = new Spr3Header()
            {
                entryOffset = entryOffsetsOffset,
                entryCount = (short)_entries.Count,
                imgOffset = imgOffset,
                imgCount = (short)_ctpkStates.Count
            };
            output.Position = 0;
            typeWriter.Write(header, bw);
        }

        #region Private Helpers

        private static IList<Spr3Offset> ReadOffsets(Stream stream, BinaryTypeReader reader, int offset, short count)
        {
            stream.Position = offset;
            return reader.ReadMany<Spr3Offset>(new BinaryReaderX(stream), count);
        }

        private static IList<byte[]> ReadEntries(Stream input, BinaryReaderX br, IEnumerable<Spr3Offset> entryOffsets)
        {
            var entries = new List<byte[]>();
            foreach (var entryOffset in entryOffsets)
            {
                input.Position = entryOffset.offset;
                entries.Add(br.ReadBytes(0x80));
            }
            return entries;
        }

        private IEnumerable<ImageFileInfo> LoadCtpk(Stream input, IPluginFileManager manager, IList<Spr3Offset> ctpkOffsets, int index)
        {
            var currentOffset = ctpkOffsets[index].offset;
            long nextOffset = (index + 1) < ctpkOffsets.Count ? ctpkOffsets[index + 1].offset : input.Length;
            var length = nextOffset - currentOffset;

            using var ctpkStream = new SubStream(input, currentOffset, length);
            var loadResult = manager.LoadFile(new StreamFile { Stream = ctpkStream, Path = "file.ctpk" }, CtpkId).Result;
            if (loadResult.Status != LoadStatus.Successful)
            {
                throw new InvalidOperationException(loadResult.Status.ToString());
            }

            _ctpkStates.Add(loadResult.LoadedFileState);
            var pluginState = loadResult.LoadedFileState.PluginState as IImageFilePluginState;
            return (IEnumerable<ImageFileInfo>)pluginState.Images;
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
            else
            {
                var saveResult = manager.SaveStream(ctpkState).Result;
                if (!saveResult.IsSuccessful)
                {
                    throw new InvalidOperationException(saveResult.ToString());
                }
                return saveResult.SavedStreams[0].Stream;
            }
        }

        private List<int> WriteEntries(Stream output, BinaryWriterX bw, int entryStartPosition)
        {
            var entryOffsets = new List<int>();
            output.Position = entryStartPosition;

            foreach (var entry in _entries)
            {
                entryOffsets.Add((int)output.Position);
                bw.Write(entry);
            }
            return entryOffsets;
        }

        private static void WriteOffsetTable(Stream output, BinaryWriterX bw, int tableStartPosition, List<int> offsets)
        {
            output.Position = tableStartPosition;
            foreach (var off in offsets)
            {
                // Write a placeholder zero followed by the actual offset.
                bw.Write(0);
                bw.Write(off);
            }
        }

        #endregion
    }
}
