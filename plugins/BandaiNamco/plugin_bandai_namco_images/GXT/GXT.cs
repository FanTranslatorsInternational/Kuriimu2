using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Palette;
using Komponent.IO;

namespace plugin_bandai_namco_images.GXT
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GXT
    {

        /// <summary>
        /// 
        /// </summary>
        public FileHeader Header;

        /// <summary>
        /// 
        /// </summary>
        public string FileName;

        /// <summary>
        /// 
        /// </summary>
        public Bitmap Texture;

        /// <summary>
        /// 
        /// </summary>
        public IList<Color> Palette;

        /// <summary>
        /// 
        /// </summary>
        public string FormatName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public GXT(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Header
                Header = br.ReadType<FileHeader>();

                // Setup
                var paletteDataLength = 256 * 4;

                // Image
                var texture = br.ReadBytes(Header.ImageDataSize);

                // Palette
                var palette = br.ReadBytes(paletteDataLength);

                var settings = new PaletteImageSettings(Formats[ImageFormat.RGBA8888], PaletteFormats[ImageFormat.Palette_8], Header.Width, Header.Height);
                var data = Common.Load(texture, palette, settings);
                Texture = data.image;
                Palette = data.palette;
                FormatName = PaletteFormats[ImageFormat.Palette_8].FormatName;

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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<ImageFormat, IImageFormat> Formats = new Dictionary<ImageFormat, IImageFormat>
        {
            [ImageFormat.RGBA8888] = new RGBA(8, 8, 8, 8) { ByteOrder = ByteOrder.BigEndian }
        };

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<ImageFormat, IPaletteImageFormat> PaletteFormats = new Dictionary<ImageFormat, IPaletteImageFormat>
        {
            [ImageFormat.Palette_8] = new Index(8, new RGBA(8, 8, 8))
        };
    }
}
