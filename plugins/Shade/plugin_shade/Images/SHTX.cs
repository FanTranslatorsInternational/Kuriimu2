using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_shade.Images
{
    public class SHTX
    {
        private ShtxHeader _header;
        private int paletteDataLength = 256 * 4;
        private byte[] palette;
        private byte[] _unkChunk;
        private int textureDataLength;
        public ImageInfo Load(Stream input)
        {

            using (var br = new BinaryReaderX(input))
            {
                // Header
                _header = br.ReadType<ShtxHeader>();

                // Get image data
                switch (_header.Format)
                {
                    case 0x4646:
                        textureDataLength = _header.Width * _header.Height * 4;
                        break;
                    case 0x3446:
                        paletteDataLength = 16 * 4;
                        palette = br.ReadBytes(paletteDataLength);
                        _unkChunk = br.ReadBytes(240 * 4); // For some reason SHTXF4's have space for 240 other colors, it's sometimes used for other things, saves it
                        textureDataLength = (_header.Width * _header.Height) / 2;
                        break;
                    default:
                        textureDataLength = _header.Width * _header.Height;
                        palette = br.ReadBytes(paletteDataLength);
                        break;
                }
                var textureData = br.ReadBytes(textureDataLength);


                var imageInfo = new ImageInfo(textureData, _header.Format, new Size(_header.Width, _header.Height));
                if (_header.Format == 0x4646)
                    return imageInfo;

                imageInfo.PaletteData = palette;
                imageInfo.PaletteFormat = 0;

                return imageInfo;
            }
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using (var bw = new BinaryWriterX(output))
            {
                _header.Width = (short)imageInfo.ImageSize.Width;
                _header.Height = (short)imageInfo.ImageSize.Height;

                bw.WriteType(_header);
                if (_header.Format == 0x4646)
                {
                    bw.Write(imageInfo.ImageData);
                }
                else
                {
                    bw.Write(imageInfo.PaletteData);

                    // In case the quantized image has a palette size that doesn't match the number of colors in the format
                    var missingColors = paletteDataLength - imageInfo.PaletteData.Length;
                    bw.WritePadding(missingColors);

                    if (_unkChunk != null)
                        bw.Write(_unkChunk);
                    bw.Write(imageInfo.ImageData);
                }


            }
        }
    }
}