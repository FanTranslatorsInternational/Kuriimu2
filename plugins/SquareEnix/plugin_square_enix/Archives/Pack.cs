using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_square_enix.Archives
{
    class Pack
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PackHeader));

        private PackHeader _header;
        private IList<long> _unknownValues;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Decide ByteOrder
            input.Position = 0xA;
            var byteOrder = (ByteOrder)br.ReadInt16();
            br.ByteOrder = byteOrder;

            // Read header
            input.Position = 0;
            _header = br.ReadType<PackHeader>();

            // Detect unsupported packs with differencing length information
            if (input.Length != _header.size)
                throw new InvalidOperationException("This PACK is not supported.");

            // Read offsets
            var offsets = br.ReadMultiple<int>(_header.fileCount);

            // Read unknown longs
            _unknownValues = br.ReadMultiple<long>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var offset in offsets)
            {
                input.Position = offset;
                var entry = br.ReadType<FileEntry>();

                var subStream = new SubStream(input, offset + entry.fileStart, entry.fileSize);
                input.Position = offset + 8;
                var name = br.ReadCStringASCII();

                result.Add(new PackArchiveFileInfo(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output, _header.byteOrder);

            // Calculate offsets
            var sizeOffset = HeaderSize;
            var unknownValueOffset = sizeOffset + files.Count * 4;
            var dataOffset = unknownValueOffset + files.Count * 8;

            // Write files
            var offsets = new List<int>();
            foreach (var file in files.Cast<PackArchiveFileInfo>())
            {
                offsets.Add(dataOffset);

                // Write file name
                output.Position = dataOffset + 8;
                bw.WriteString(file.FilePath.GetName(), Encoding.ASCII, false);

                // Pad to file start
                var npad = dataOffset + file.Entry.fileStart - output.Position;
                bw.WritePadding((int)npad);;

                // Write file data
                output.Position = dataOffset + file.Entry.fileStart;
                var writtenSize = file.SaveFileData(output);
                var nextOffset = output.Position;

                // Write file entry
                file.Entry.fileSize = (uint)writtenSize;
                output.Position = dataOffset;
                bw.WriteType(file.Entry);

                dataOffset = (int)nextOffset;
            }

            // Write unknown values
            output.Position = unknownValueOffset;
            bw.WriteMultiple(_unknownValues);

            // Write offsets
            output.Position = sizeOffset;
            bw.WriteMultiple(offsets);

            // Write header
            _header.fileCount = (short)files.Count;
            _header.size = (int)output.Length;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
