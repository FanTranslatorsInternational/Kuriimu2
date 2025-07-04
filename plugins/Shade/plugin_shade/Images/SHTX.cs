using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_shade.Images
{
    public class SHTX
    {
        private ShtxHeader _header;
        private int paletteDataLength = 256 * 4;
        private byte[] palette;
        private byte[] _unkChunk;
        private int textureDataLength;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Header
            _header = ReadHeader(br);

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
                    textureDataLength = _header.Width * _header.Height / 2;
                    break;

                default:
                    textureDataLength = _header.Width * _header.Height;
                    palette = br.ReadBytes(paletteDataLength);
                    break;
            }

            var textureData = br.ReadBytes(textureDataLength);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = !ShtxSupport.EncodingsV1.TryGetValue(_header.Format, out var encoding)
                    ? ShtxSupport.IndexEncodings[_header.Format].IndexEncoding.BitDepth
                    : encoding.BitsPerValue,
                ImageData = textureData,
                ImageFormat = _header.Format,
                ImageSize = new Size(_header.Width, _header.Height)
            };

            if (_header.Format == 0x4646)
                return imageInfo;

            imageInfo.PaletteData = palette;
            imageInfo.PaletteFormat = 0;

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            _header.Width = (short)imageInfo.ImageSize.Width;
            _header.Height = (short)imageInfo.ImageSize.Height;

            WriteHeader(_header, bw);

            if (_header.Format != 0x4646)
            {
                bw.Write(imageInfo.PaletteData);

                // In case the quantized image has a palette size that doesn't match the number of colors in the format
                var missingColors = paletteDataLength - imageInfo.PaletteData.Length;
                bw.WritePadding(missingColors);

                if (_unkChunk != null)
                    bw.Write(_unkChunk);
            }

            bw.Write(imageInfo.ImageData);
        }

        private ShtxHeader ReadHeader(BinaryReaderX reader)
        {
            return new ShtxHeader
            {
                Magic = reader.ReadString(4),
                Format = reader.ReadInt16(),
                Width = reader.ReadInt16(),
                Height = reader.ReadInt16(),
                LogW = reader.ReadByte(),
                LogH = reader.ReadByte()
            };
        }

        private void WriteHeader(ShtxHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.Magic, writeNullTerminator: false);
            writer.Write(header.Format);
            writer.Write(header.Width);
            writer.Write(header.Height);
            writer.Write((byte)Math.Ceiling(Math.Log(header.Width, 2)));
            writer.Write((byte)Math.Ceiling(Math.Log(header.Height, 2)));
        }
    }
}
