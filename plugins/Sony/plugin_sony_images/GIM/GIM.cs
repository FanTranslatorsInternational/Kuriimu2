using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;

namespace plugin_sony_images.GIM
{
    public sealed class GIM
    {
        public List<List<Bitmap>> Images { get; set; }

        public GIM(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                var magic = br.ReadBytes(0x10);
                var root = new RootBlock(input);

                Images = root.PictureBlocks.SelectMany(p => p.ImageBlocks.Select(i => i.Bitmaps)).ToList();
            }
        }
    }
}
