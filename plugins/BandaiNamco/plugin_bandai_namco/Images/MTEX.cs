using System.Drawing;
using System.IO;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_bandai_namco.Images
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MTEX
    {
        private MtexHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<MtexHeader>();

            // Ignore padding
            br.BaseStream.Position = 0x80;

            // Read texture
            var texture = br.ReadBytes((int)input.Length - 0x80);

            var imageInfo = new ImageInfo(texture, _header.format, new Size(_header.width, _header.height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context,CtrTransformation.YFlip));
            imageInfo.PadSize.ToPowerOfTwo();

            return imageInfo;
        }

        public void Save(Stream output, IKanvasImage image)
        {
            using var bw = new BinaryWriterX(output);

            // Header
            _header.width = (short)image.ImageSize.Width;
            _header.height = (short)image.ImageSize.Height;
            _header.format = (byte)image.ImageFormat;

            // Writing
            bw.WriteType(_header);
            bw.BaseStream.Position = 0x80;
            bw.Write(image.ImageInfo.ImageData);
        }
    }
}
