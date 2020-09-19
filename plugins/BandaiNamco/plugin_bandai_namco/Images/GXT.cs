using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_bandai_namco.Images
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GXT
    {
        private const int PaletteDataLength_ = 256 * 4;

        private GxtHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<GxtHeader>();

            // Read texture
            var texture = br.ReadBytes(_header.ImageDataSize);

            // Read palette
            var palette = br.ReadBytes(PaletteDataLength_);

            return new ImageInfo(texture, 5, new Size(_header.Width, _header.Height))
            {
                PaletteData = palette,
                PaletteFormat = 0,
            };

            //if (Header.Format == ImageFormat.Palette_8)
            //{
            //}
            //else
            //{
            //    var settings = new ImageSettings(Formats[ImageFormat.RGBA8888], Header.Width, Header.Height);
            //    Texture = Common.Load(texture, settings);
            //    FormatName = Formats[ImageFormat.RGBA8888].FormatName;
            //}
        }

        public void Save(Stream output, IKanvasImage image)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
            }
        }
    }
}
