using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Models.Archive;

namespace plugin_shade.Archives
{
    class Bin
    {
        public IList<IArchiveFileInfo> Load(Stream input) 
        {
            using var br = new BinaryReaderX(input, true);
            
            // Read Header
            var header = br.ReadType<BinHeader>();

            // Read entries
            var entries = br.ReadMultiple<BinFileInfo>(header.fileCount);

            var files = new List<IArchiveFileInfo>();
            var index = 0;
            foreach(var entry in entries)
            {
                var offset = (entry.offSize >> header.shiftFactor) * header.padFactor;
                var size = (entry.offSize & header.mask) * header.mulFactor;

                var stream = new SubStream(input, offset, size);
                files.Add(CreateAfi(stream, index++, entry));

            }
            return files;

        }
        private ArchiveFileInfo CreateAfi(Stream stream, int index, BinFileInfo entry)
        {
            // Every file not compressed with the headered Spike Chunsoft compression, is compressed headerless
            var compressionMagic = ShadeSupport.PeekInt32LittleEndian(stream);
            if (compressionMagic != 0xa755aafc)
                return new BinArchiveFileInfo(stream, ShadeSupport.CreateFileName(index, stream, false), entry, Kompression.Implementations.Compressions.SpikeChunsoftHeaderless, SpikeChunsoftHeaderlessDecoder.CalculateDecompressedSize(stream));

            stream.Position = 0;
            return new BinArchiveFileInfo(stream, ShadeSupport.CreateFileName(index, stream, true), entry, Kompression.Implementations.Compressions.SpikeChunsoft, ShadeSupport.PeekDecompressedSize(stream));

        }
    }
}
