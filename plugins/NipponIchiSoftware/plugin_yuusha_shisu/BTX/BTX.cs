using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Palette;
using Komponent.IO;

namespace plugin_yuusha_shisu.BTX
{
    /// <summary>
    /// 
    /// </summary>
    public class BTX
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
        public BTX(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Header
                Header = br.ReadType<FileHeader>();
                br.SeekAlignment();
                FileName = br.ReadCStringASCII();

                // Setup
                var dataLength = Header.Width * Header.Height;
                var paletteDataLength = Header.ColorCount * 4;

                // Image
                br.BaseStream.Position = Header.ImageOffset;
                var texture = br.ReadBytes(dataLength);

                // Palette
                if (Header.Format == ImageFormat.Palette_8)
                {
                    br.BaseStream.Position = Header.PaletteOffset;
                    var palette = br.ReadBytes(paletteDataLength);

                    var settings = new PaletteImageSettings(Formats[ImageFormat.RGBA8888], PaletteFormats[ImageFormat.Palette_8], Header.Width, Header.Height);
                    var data = Common.Load(texture, palette, settings);
                    Texture = data.image;
                    Palette = data.palette;
                    FormatName = PaletteFormats[ImageFormat.Palette_8].FormatName;
                }
                else
                {
                    var settings = new ImageSettings(Formats[ImageFormat.RGBA8888], Header.Width, Header.Height);
                    Texture = Common.Load(texture, settings);
                    FormatName = Formats[ImageFormat.RGBA8888].FormatName;
                }
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
                // Updates
                Header.Width = (short)Texture.Width;
                Header.Height = (short)Texture.Height;

                // Header
                bw.WriteType(Header);
                bw.WriteAlignment();
                bw.WriteString(FileName, Encoding.ASCII, false);
                bw.WriteAlignment();

                // Setup
                if (Header.Format == ImageFormat.Palette_8)
                {
                    var settings = new PaletteImageSettings(Formats[ImageFormat.RGBA8888], PaletteFormats[ImageFormat.Palette_8], Header.Width, Header.Height);
                    var data = Common.Save(Texture, Palette, settings);

                    bw.Write(data.indexData);
                    bw.Write(data.paletteData);
                }
                else
                {
                    var settings = new ImageSettings(Formats[ImageFormat.RGBA8888], Header.Width, Header.Height);
                    var data = Common.Save(Texture, settings);

                    bw.Write(data);
                }
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
