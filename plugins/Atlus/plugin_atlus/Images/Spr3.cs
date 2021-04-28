using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models;

namespace plugin_atlus.Images
{
    class Spr3
    {
        private static readonly Guid CtpkId = Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");

        private static readonly int HeaderSize = Tools.MeasureType(typeof(Spr3Header));
        private static readonly int OffsetSize = Tools.MeasureType(typeof(Spr3Offset));

        private Spr3Header _header;
        private IList<byte[]> _entries;
        private IList<IFileState> _ctpkStates;

        public IList<IKanvasImage> Load(Stream input, IFileManager manager)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<Spr3Header>();

            // Read CTPK offsets
            input.Position = _header.imgOffset;
            var ctpkOffsets = br.ReadMultiple<Spr3Offset>(_header.imgCount).Select(x => x.offset).ToArray();

            // Read entry offsets
            input.Position = _header.entryOffset;
            var entryOffsets = br.ReadMultiple<Spr3Offset>(_header.entryCount).Select(x => x.offset).ToArray();

            // Read entries
            _entries = new List<byte[]>();
            foreach (var entryOffset in entryOffsets)
            {
                input.Position = entryOffset;
                _entries.Add(br.ReadBytes(0x80));
            }

            // Load CTPKs
            var result = new List<IKanvasImage>();

            _ctpkStates = new List<IFileState>();
            for (var i = 0; i < ctpkOffsets.Length; i++)
            {
                var ctpkOffset = ctpkOffsets[i];
                var nextOffset = i + 1 >= ctpkOffsets.Length ? input.Length : ctpkOffsets[i + 1];
                var ctpkStream = new SubStream(input, ctpkOffset, nextOffset - ctpkOffset);

                var loadResult = manager.LoadFile(new StreamFile(ctpkStream, "file.ctpk"), CtpkId).Result;
                if (!loadResult.IsSuccessful)
                    throw new InvalidOperationException(loadResult.Message);

                _ctpkStates.Add(loadResult.LoadedFileState);
                result.AddRange((loadResult.LoadedFileState.PluginState as IImageState).Images);
            }

            return result;
        }

        public void Save(Stream output, IFileManager manager)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var imgOffset = HeaderSize;
            var entryOffsetsOffset = imgOffset + _ctpkStates.Count * OffsetSize;
            var entryOffset = entryOffsetsOffset + _entries.Count * OffsetSize;
            var dataOffset = entryOffset + _entries.Sum(x => x.Length);

            // Write CTPKs
            var imgOffsets = new List<int>();

            var dataPosition = dataOffset;
            foreach (var ctpkState in _ctpkStates)
            {
                // Save CTPK
                Stream ctpkStream;
                if (!ctpkState.StateChanged)
                    ctpkStream = ctpkState.FileSystem.OpenFile(ctpkState.FilePath);
                else
                {
                    var saveResult = manager.SaveStream(ctpkState).Result;
                    if (!saveResult.IsSuccessful)
                        throw new InvalidOperationException(saveResult.Message);

                    ctpkStream = saveResult.SavedStream[0].Stream;
                }

                // Write CTPK
                output.Position = dataPosition;

                ctpkStream.Position = 0;
                ctpkStream.CopyTo(output);

                // Update meta information
                imgOffsets.Add(dataPosition);
                dataPosition += (int)ctpkStream.Length;
            }

            // Write entries
            var entryOffsets = new List<int>();

            output.Position = entryOffset;
            foreach (var entry in _entries)
            {
                entryOffsets.Add((int)output.Position);
                bw.Write(entry);
            }

            // Write entry offsets
            output.Position = entryOffsetsOffset;
            foreach (var entryOff in entryOffsets)
            {
                bw.Write(0);
                bw.Write(entryOff);
            }

            // Write CTPK offsets
            output.Position = imgOffset;
            foreach (var imgOff in imgOffsets)
            {
                bw.Write(0);
                bw.Write(imgOff);
            }

            // Write header
            _header.entryOffset = entryOffsetsOffset;
            _header.entryCount = (short)_entries.Count;
            _header.imgOffset = imgOffset;
            _header.imgCount = (short)_ctpkStates.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
