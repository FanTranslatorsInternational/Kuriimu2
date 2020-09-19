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

            var imageInfo = new ImageInfo(texture, _header.Format, new Size(_header.Width, _header.Height))
            {
                Configuration = new ImageConfiguration().
                        RemapPixelsWith(size => new CTRSwizzle(_header.Width, _header.Height, CtrTransformation.YFlip, true)),
            };

            return imageInfo;
        }

        public void Save(Stream output, IKanvasImage image)
        {
            var bw = new BinaryWriterX(output);
            
            // Header
            _header.Width = (short)image.ImageSize.Width;
            _header.Height = (short)image.ImageSize.Height;
            _header.Format = (byte)image.ImageFormat;                
            
            bw.WriteType(_header);
            bw.BaseStream.Position = 0x80;
            bw.Write(image.ImageInfo.ImageData);            
        }
    }
}
