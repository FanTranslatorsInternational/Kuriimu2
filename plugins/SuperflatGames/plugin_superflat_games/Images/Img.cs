using System.Drawing;
using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Models.Image;

namespace superflat_games.Images
{
    class Img
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(ImgHeader));

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read image data
            var imgHeader = br.ReadType<ImgHeader>();
            var imgData = br.ReadBytes(imgHeader.size);

            // Read tex info
            var texHeader = br.ReadType<ImgHeader>();
            var texInfo = br.ReadType<ImgEntry>();

            return new ImageInfo(imgData, 0, new Size(texInfo.width, texInfo.height));
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var texOffset = HeaderSize + imageInfo.ImageData.Length;

            // Write image data
            bw.WriteType(new ImgHeader { magic = "IMG0", size = imageInfo.ImageData.Length });
            bw.Write(imageInfo.ImageData);

            // Write tex header
            bw.WriteType(new ImgHeader { magic = "TEXR", size = 0x10 });
            bw.WriteType(new ImgEntry { width = imageInfo.ImageSize.Width, height = imageInfo.ImageSize.Height });

            // Write end header
            bw.WriteString("!END", Encoding.ASCII, false, false);
            bw.WritePadding(3);
        }
    }
}
