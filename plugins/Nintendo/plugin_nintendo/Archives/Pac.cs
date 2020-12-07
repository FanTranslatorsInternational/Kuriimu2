using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class Pac
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(PacEntry));

        private PacHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = br.ReadType<PacHeader>();

            // Read table info
            var tableInfo = br.ReadType<PacTableInfo>();

            // Read assets
            input.Position = tableInfo.assetOffset;
            var assets = br.ReadMultiple<PacAsset>(tableInfo.assetCount);

            // Read entries
            input.Position = tableInfo.entryOffset;
            var entries = br.ReadMultiple<PacEntry>(tableInfo.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var asset in assets)
            {
                input.Position = asset.stringOffset;
                var assetName = br.ReadCStringASCII();

                var entryStartCount = (asset.entryOffset - tableInfo.assetOffset) / EntrySize;
                foreach (var entry in entries.Skip(entryStartCount).Take(asset.count))
                {
                    input.Position = entry.stringOffset;
                    var entryName = br.ReadCStringASCII();

                    var subStream = new SubStream(input, entry.offset, entry.compSize);
                    var fileName = assetName + "/" + entryName;

                    result.Add(new PacArchiveFileInfo(subStream, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.decompSize));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {

        }
    }
}
