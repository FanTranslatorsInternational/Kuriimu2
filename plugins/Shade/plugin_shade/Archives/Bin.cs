using System.Collections.Generic;
using System.Linq;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Models.Archive;

namespace plugin_shade.Archives
{
    class Bin
    {
        BinHeader _header;
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read Header
            _header = br.ReadType<BinHeader>();

            // Read entries
            var entries = br.ReadMultiple<BinFileInfo>(_header.fileCount);

            // Read files
            var files = new List<IArchiveFileInfo>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var offset = (entry.offSize >> _header.shiftFactor) * _header.padFactor;
                var size = (entry.offSize & _header.mask) * _header.mulFactor;

                var stream = new SubStream(input, offset, size);
                files.Add(CreateAfi(stream, i, entry));
            }

            return files;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);
            var castedFiles = files.Cast<BinArchiveFileInfo>();

            // Write files
            foreach (var file in castedFiles)
            {
                var offset = (file.Entry.offSize >> _header.shiftFactor) * _header.padFactor;
                output.Position = offset;

                file.SaveFileData(output);
            }
            bw.WriteAlignment(_header.padFactor);

            // Write header
            output.Position = 0;
            bw.WriteType(_header);

            // Write entries
            foreach (var file in castedFiles)
                bw.Write(file.Entry.offSize);
        }

        private ArchiveFileInfo CreateAfi(Stream stream, int index, BinFileInfo entry)
        {
            // Every file not compressed with the headered Spike Chunsoft compression, is compressed headerless
            var compressionMagic = ShadeSupport.PeekInt32LittleEndian(stream);
            if (compressionMagic != 0xa755aafc)
                return new BinArchiveFileInfo(stream, ShadeSupport.CreateFileName(index, stream, false), entry, Kompression.Implementations.Compressions.ShadeLzHeaderless, SpikeChunsoftHeaderlessDecoder.CalculateDecompressedSize(stream));

            stream.Position = 0;
            return new BinArchiveFileInfo(stream, ShadeSupport.CreateFileName(index, stream, true), entry, Kompression.Implementations.Compressions.ShadeLz, ShadeSupport.PeekDecompressedSize(stream));

        }
    }
}