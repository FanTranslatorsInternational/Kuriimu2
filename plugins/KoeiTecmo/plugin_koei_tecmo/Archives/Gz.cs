using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    class Gz
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(GzHeader));

        private GzHeader _header;
        private IList<int> _blockSizes;
        private Stream _origStream;

        public IArchiveFileInfo Load(Stream input, string fileName = null)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<GzHeader>();

            // Read sizes
            _blockSizes = br.ReadMultiple<int>(_header.blockCount);
            var blockOffsets = new int[_header.blockCount];
            for (var i = 0; i < _header.blockCount; i++)
            {
                input.Position = (input.Position + 0x7F) & ~0x7F;
                blockOffsets[i] = (int)(input.Position + 4);
                input.Position += _blockSizes[i];
            }

            // Create file
            _origStream = new SubStream(input, blockOffsets[0], input.Length - blockOffsets[0] - 4);
            var fileStream = new GzStream(input, _header.decompBlockSize, _header.decompSize, blockOffsets.Zip(_blockSizes.Select(x => x - 4)).ToArray());
            fileName ??= "00000000.bin";

            return new ArchiveFileInfo(fileStream, fileName);
        }

        public void Save(Stream output, IArchiveFileInfo file)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var sizeOffset = HeaderSize;
            var blockCount = file.ContentChanged ? (int)Math.Ceiling(file.FileSize / (float)_header.decompBlockSize) : _blockSizes.Count;
            var dataOffset = (sizeOffset + blockCount * 4 + 0x7F) & ~0x7F;

            // Chunk stream into blocks
            output.Position = dataOffset;
            var blockSizes = _blockSizes;

            if (file.ContentChanged)
            {
                blockSizes = GzStream.ChunkStream(file.GetFileData().Result, output, _header.decompBlockSize, 0x80);
                _header.decompSize = (int)file.FileSize;
            }
            else
            {
                _origStream.Position = 0;
                _origStream.CopyTo(output);
            }

            // Write header
            _header.blockCount = blockSizes.Count;

            output.Position = 0;
            bw.WriteType(_header);

            // Write block sizes
            bw.WriteMultiple(blockSizes);
        }
    }
}
