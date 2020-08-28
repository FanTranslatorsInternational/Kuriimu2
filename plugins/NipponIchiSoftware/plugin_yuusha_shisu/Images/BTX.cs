using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_yuusha_shisu.Images
{
    public class BTX
    {
        public ImageInfo Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Header
                var header = br.ReadType<FileHeader>();
                br.SeekAlignment();
                var fileName = br.ReadCStringASCII();

                // Setup
                var dataLength = header.Width * header.Height;
                var paletteDataLength = header.ColorCount * 4;

                // Image
                br.BaseStream.Position = header.ImageOffset;
                var texture = br.ReadBytes(dataLength);

                // Palette
                var imageInfo = new ImageInfo(texture, (int)header.Format, new Size(header.Width, header.Height))
                {
                    Name = fileName
                };

                if (header.Format != ImageFormat.Palette_8) 
                    return imageInfo;

                br.BaseStream.Position = header.PaletteOffset;
                var palette = br.ReadBytes(paletteDataLength);

                imageInfo.PaletteFormat = (int)header.Format;
                imageInfo.PaletteData = palette;

                return imageInfo;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        //public void Save(Stream output)
        //{
        //    using (var bw = new BinaryWriterX(output, true))
        //    {
        //        // Updates
        //        Header.Width = (short)Texture.Width;
        //        Header.Height = (short)Texture.Height;

        //        // Header
        //        bw.WriteType(Header);
        //        bw.WriteAlignment();
        //        bw.WriteString(FileName, Encoding.ASCII, false);
        //        bw.WriteAlignment();

        //        // Setup
        //        if (Header.Format == ImageFormat.Palette_8)
        //        {
        //            var settings = new IndexedImageSettings(IndexEncodings[(int)Header.Format], PaletteEncodings[(int)Header.Format], Header.Width, Header.Height)
        //            {
        //                QuantizationSettings = new QuantizationSettings(new WuColorQuantizer(6, 3), Header.Width, Header.Height)
        //                {
        //                    ColorCount = 256,
        //                    ParallelCount = 8
        //                }
        //            };
        //            var data = Kolors.Save(Texture, settings);

        //            bw.Write(data.indexData);
        //            bw.Write(data.paletteData);
        //        }
        //        else
        //        {
        //            var settings = new ImageSettings(Encodings[(int)Header.Format], Header.Width, Header.Height);
        //            var data = Kolors.Save(Texture, settings);

        //            bw.Write(data);
        //        }
        //    }
        //}
    }
}
