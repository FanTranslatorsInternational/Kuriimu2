using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.Models.Images;

namespace plugin_nintendo.Images
{
    class Ctpk
    {
        public IList<ImageInfo> Load(Stream input)
        {
            using (BinaryReaderX br = new BinaryReaderX(input))
            {
                //Header
                var header = br.ReadType<Header>();
                var entries = new List<CtpkEntry>();
                for (var i = 0; i < header.texCount; i++)
                    entries.Add(new CtpkEntry());

                //TexEntry List
                br.BaseStream.Position = 0x20;
                foreach (var entry in entries)
                    entry.texEntry = br.ReadType<TexEntry>();

                //DataSize List
                foreach (var entry in entries)
                    for (var i = 0; i < entry.texEntry.mipLvl; i++)
                        entry.dataSizes.Add(br.ReadInt32());

                //Name List
                foreach (var entry in entries)
                    entry.name = br.ReadCStringASCII();

                //Hash List
                br.BaseStream.Position = header.crc32SecOffset;
                var hashList = br.ReadMultiple<HashEntry>(header.texCount).OrderBy(e => e.id).ToList();
                var count = 0;
                foreach (var entry in entries)
                    entry.hash = hashList[count++];

                //MipMapInfo List
                br.BaseStream.Position = header.texInfoOffset;
                foreach (var entry in entries)
                    entry.mipmapEntry = br.ReadType<MipmapEntry>();

                //Add bmps
                var result = new List<ImageInfo>();
                br.BaseStream.Position = header.texSecOffset;
                for (var i = 0; i < entries.Count; i++)
                {
                    // Main texture
                    br.BaseStream.Position = entries[i].texEntry.texOffset + header.texSecOffset;

                    var dataSize = entries[i].dataSizes[0] == 0
                        ? entries[i].texEntry.texDataSize
                        : entries[i].dataSizes[0];
                    result.Add(new ImageInfo
                    {
                        ImageData = br.ReadBytes(dataSize),
                        ImageFormat = entries[i].texEntry.imageFormat,
                        ImageSize = new Size(entries[i].texEntry.width, entries[i].texEntry.height),
                        MipMapData = Enumerable.Range(1, entries[i].texEntry.mipLvl - 1)
                            .Select(x => br.ReadBytes(entries[i].dataSizes[x]))
                            .ToArray(),
                        Configuration = new ImageConfiguration()
                            .RemapPixelsWith(size => new CTRSwizzle(size.Width, size.Height, CtrTransformation.None, true))
                    });
                }

                return result;
            }
        }

        public void Save(IList<ImageInfo> imageInfos, Stream output)
        {

        }
    }
}
