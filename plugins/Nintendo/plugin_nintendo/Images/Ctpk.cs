using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class Ctpk
    {
        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<CtpkHeader>();

            // Read tex entries
            br.BaseStream.Position = 0x20;
            var texEntries = br.ReadMultiple<TexEntry>(header.texCount);

            // Read data sizes
            var dataSizes = new int[header.texCount][];
            for (var i = 0; i < header.texCount; i++)
                dataSizes[i] = br.ReadMultiple<int>(texEntries[i].mipLvl).ToArray();

            // Read names
            var names = new string[header.texCount];
            for (var i = 0; i < header.texCount; i++)
                names[i] = br.ReadCStringASCII();

            // Read hash entries
            br.BaseStream.Position = header.crc32SecOffset;
            var hashEntries = br.ReadMultiple<HashEntry>(header.texCount).OrderBy(x => x.id).ToArray();

            // Read mip map infos
            br.BaseStream.Position = header.texInfoOffset;
            var mipMapEntries = br.ReadMultiple<MipmapEntry>(header.texCount);

            // Add images
            var result = new ImageInfo[header.texCount];
            for (var i = 0; i < header.texCount; i++)
            {
                // Read image data
                br.BaseStream.Position = header.texSecOffset + texEntries[i].texOffset;
                var imageData = br.ReadBytes(texEntries[i].texDataSize);

                // Read mip maps
                var mipMaps = Enumerable.Range(1, texEntries[i].mipLvl - 1)
                    .Select(x => br.ReadBytes(dataSizes[i][x]))
                    .ToArray();

                result[i] = new ImageInfo(imageData, mipMaps, texEntries[i].imageFormat, new Size(texEntries[i].width, texEntries[i].height))
                {
                    Name = names[i]
                };
                result[i].RemapPixels.With(context => new CtrSwizzle(context));
                result[i].PadSize.ToPowerOfTwo();
            }

            return result;
        }

        public void Save(Stream output, IList<IKanvasImage> images)
        {

        }
    }
}
