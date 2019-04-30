using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Kanvas;
using Kanvas.Encoding;
using Kanvas.IndexEncoding;
using Kanvas.Interface;
using Kanvas.Models;
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
        public string FormatName;

        /// <summary>
        /// 
        /// </summary>
        public bool HasPalette { get; }

        /// <summary>
        /// 
        /// </summary>
        public IList<Color> Palette;

        /// <summary>
        /// 
        /// </summary>
        public string PaletteFormatName;

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

                    var settings = new IndexImageSettings(IndexEncodings[0], Encodings[0], Header.Width, Header.Height);
                    var data = Kolors.Load(texture, palette, settings);
                    Texture = data.image;
                    Palette = data.palette;
                    FormatName = Encodings[0].FormatName;
                    PaletteFormatName = IndexEncodings[0].FormatName;
                    HasPalette = true;
                }
                else
                {
                    var settings = new ImageSettings(Encodings[0], Header.Width, Header.Height);
                    Texture = Kolors.Load(texture, settings);
                    FormatName = Encodings[0].FormatName;
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
                    var settings = new IndexImageSettings(IndexEncodings[0], Encodings[0], Header.Width, Header.Height);
                    var data = Kolors.Save(Texture, settings);

                    bw.Write(data.indexData);
                    bw.Write(data.paletteData);
                }
                else
                {
                    var settings = new ImageSettings(Encodings[0], Header.Width, Header.Height);
                    var data = Kolors.Save(Texture, settings);

                    bw.Write(data);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<int, IColorEncoding> Encodings = new Dictionary<int, IColorEncoding>
        {
            [0] = new RGBA(8, 8, 8, 8) { ByteOrder = Kanvas.Models.ByteOrder.BigEndian }
        };

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<int, IIndexEncoding> IndexEncodings = new Dictionary<int, IIndexEncoding>
        {
            [0] = new Index(8, true)
        };
    }
}
