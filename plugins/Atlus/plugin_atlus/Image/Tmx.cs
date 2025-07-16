using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;
using System.Text;
using Kanvas.Contract.Encoding;

namespace plugin_atlus.Image
{
    /* Original understanding by xdaniel and his tool Tharsis
     * https://github.com/xdanieldzd/Tharsis */

    class Tmx
    {
        private static readonly int HeaderSize = 0x24;
        private string _comment;

        private TmxHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);
            _comment = br.ReadString(0x1C, Encoding.ASCII);

            // Read palette
            var paletteSize = GetPaletteDataSize(_header.paletteFormat, (TMXPixelFormat)_header.imageFormat);
            var paletteData = br.ReadBytes(paletteSize);

            // Read image data
            var dataSize = GetImageDataSize(_header.imageFormat, _header.width, _header.height);
            var imageData = br.ReadBytes(dataSize);

            // Read mip data
            var mips = new List<byte[]>();
            for (var i = 1; i <= _header.mipmapCount; i++)
            {
                var mipSize = GetImageDataSize(_header.imageFormat, _header.width >> i, _header.height >> i);
                mips.Add(br.ReadBytes(mipSize));
            }

            // Create image info

            var imageInfo = new ImageFileInfo
            {
                Name = "",
                BitDepth = imageData.Length * 8 / (_header.width * _header.height),
                ImageData = imageData,
                ImageFormat = _header.imageFormat,
                ImageSize = new Size(_header.width, _header.height),
                MipMapData = _header.mipmapCount > 0 ? mips : []
            };

            if (paletteData.Length > 0)
            {
                var correctedPalette = SwizzlePaletteData(paletteData, _header.paletteFormat);

                imageInfo.PaletteData = correctedPalette;
                imageInfo.PaletteFormat = _header.paletteFormat;
            }

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var commentOffset = HeaderSize;
            var paletteOffset = commentOffset + 0x1C;
            var dataOffset = paletteOffset + (imageInfo.PaletteData != null ? GetPaletteDataSize(imageInfo.PaletteFormat, (TMXPixelFormat)imageInfo.ImageFormat) : 0);

            // Write image data
            output.Position = dataOffset;
            output.Write(imageInfo.ImageData);

            if (imageInfo.MipMapData.Count > 0)
                foreach (var mipData in imageInfo.MipMapData)
                    output.Write(mipData);

            // Write palette data
            if (imageInfo.PaletteData != null)
            {
                var correctedPalette = SwizzlePaletteData(imageInfo.PaletteData, imageInfo.PaletteFormat);

                output.Position = paletteOffset;
                output.Write(correctedPalette);
            }

            // Write comment
            output.Position = commentOffset;
            bw.WriteString(_comment, Encoding.ASCII, writeNullTerminator: false);

            // Write header
            _header.imageFormat = (byte)imageInfo.ImageFormat;
            _header.paletteFormat = (byte)(imageInfo.PaletteData != null ? imageInfo.PaletteFormat : 0);
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.fileSize = (int)output.Length;
            _header.mipmapCount = (byte)imageInfo.MipMapData.Count;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private TmxHeader ReadHeader(BinaryReaderX reader)
        {
            return new TmxHeader
            {
                unk1 = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                magic = reader.ReadString(4),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadByte(),
                paletteFormat = reader.ReadByte(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                imageFormat = reader.ReadByte(),
                mipmapCount = reader.ReadByte(),
                mipmapKValue = reader.ReadByte(),
                mipmapLValue = reader.ReadByte(),
                texWrap = reader.ReadInt16(),
                texID = reader.ReadInt32(),
                CLUTID = reader.ReadInt32()
            };
        }

        private void WriteHeader(TmxHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.fileSize);
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.paletteFormat);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.imageFormat);
            writer.Write(header.mipmapCount);
            writer.Write(header.mipmapKValue);
            writer.Write(header.mipmapLValue);
            writer.Write(header.texWrap);
            writer.Write(header.texID);
            writer.Write(header.CLUTID);
        }

        private int GetPaletteDataSize(int paletteFormat, TMXPixelFormat imageFormat)
        {
            var paletteEncoding = TmxSupport.ColorFormats[paletteFormat];

            int paletteSize = 0;
            switch (imageFormat)
            {
                case TMXPixelFormat.PSMT4:
                    paletteSize = paletteEncoding.BitDepth * 16 / 8;
                    break;

                case TMXPixelFormat.PSMT8:
                    paletteSize = paletteEncoding.BitDepth * 256 / 8;
                    break;
            }

            return paletteSize;
        }

        private int GetImageDataSize(int imageFormat, int width, int height)
        {
            var bitDepth = TmxSupport.ColorFormats.TryGetValue(imageFormat, out IColorEncoding? format) ?
                format.BitDepth :
                TmxSupport.IndexFormats[imageFormat].BitDepth;

            var dataSize = bitDepth * width * height / 8;

            return dataSize;
        }

        private byte[] SwizzlePaletteData(byte[] palette, int paletteFormat)
        {
            var newPalette = new byte[palette.Length];
            Array.Copy(palette, newPalette, palette.Length);

            var paletteEncoding = TmxSupport.ColorFormats[paletteFormat];
            var colorDepth = paletteEncoding.BitDepth / 8;

            if (newPalette.Length <= 16 * colorDepth)
                return newPalette;

            for (var i = 0; i < newPalette.Length; i += colorDepth * 32)
            {
                var rowLength = colorDepth * 8;

                var row1Index = i + rowLength;
                var row2Index = i + rowLength * 2;

                var tmp = new byte[rowLength];
                Array.Copy(newPalette, row1Index, tmp, 0, rowLength);

                Array.Copy(newPalette, row2Index, newPalette, row1Index, rowLength);
                Array.Copy(tmp, 0, newPalette, row2Index, rowLength);
            }

            return newPalette;
        }
    }
}
