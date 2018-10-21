using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Interface;
using Komponent.IO;

namespace plugin_sony_images.GIM
{
    public sealed class GIM
    {
        public List<List<(Bitmap, IImageFormat, ImageBlockMeta, IImageFormat, ImageBlockMeta)>> Images { get; set; }

        private RootBlock _root;
        private byte[] _magic;

        public GIM(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                _magic = br.ReadBytes(0x10);

                _root = new RootBlock();
                _root.Load(input);

                Images = _root.PictureBlocks.SelectMany(p => p.ImageBlocks.Select(i => i.Bitmaps)).ToList();
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                bw.Write(_magic);
                _root.Save(output, Images);
            }
        }
    }
}
