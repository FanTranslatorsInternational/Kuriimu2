using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_bandai_namco.Compression;

namespace plugin_bandai_namco.Archives
{
    class _3dsLz
    {
        private IList<int> _sizes;

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var offsets = new List<long>();
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var headerMagic = br.ReadString(6);
                if (headerMagic == "3DS-LZ")
                    offsets.Add(br.BaseStream.Position + 2);

                br.BaseStream.Position += 0x3A;
            }

            _sizes = new int[offsets.Count];
            for (var i = 0; i < offsets.Count; i++)
            {
                var endOffset = i + 1 == offsets.Count ? input.Length : (offsets[i + 1] - 8);
                _sizes[i] = (int)(endOffset - offsets[i]);
            }

            // Add files
            var result = new ArchiveFileInfo[offsets.Count];
            for (var i = 0; i < offsets.Count; i++)
            {
                var subStream = new SubStream(input, offsets[i], _sizes[i]);

                var compressionMethod = NintendoCompressor.PeekCompressionMethod(subStream);
                var decompressedSize = NintendoCompressor.PeekDecompressedSize(subStream);

                result[i] = new ArchiveFileInfo(subStream, $"{i:00000000}{_3dsLzSupport.DetermineExtension(subStream)}",
                    NintendoCompressor.GetConfiguration(compressionMethod), decompressedSize);
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            // Since this is a pointerless archive, we need to keep the original offsets in tact as much as possible

            using var bw = new BinaryWriterX(output);

            for (var i = 0; i < files.Count; i++)
            {
                var offset = output.Position;

                bw.WriteString("3DS-LZ\r\n", Encoding.ASCII, false, false);

                var writtenSize = files[i].SaveFileData(output);
                var paddedSize = (writtenSize + 0x3F) & ~0x3F;
                var finalSize = paddedSize - 8;
                if (i + 1 < files.Count && finalSize > _sizes[i])
                    throw new InvalidOperationException("Plugin can not save larger files than their original.");

                output.Position = offset + _sizes[i]+8;
            }
        }
    }
}
