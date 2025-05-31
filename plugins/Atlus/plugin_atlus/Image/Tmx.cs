using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;
using System.Text;

namespace plugin_atlus.Image
{
    /* Original understanding by xdaniel and his tool Tharsis
     * https://github.com/xdanieldzd/Tharsis */

    class Tmx
    {
        private static readonly int HeaderSize = 35;
        private string _comment;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            TmxHeader header = ReadHeader(br);
            _comment = br.ReadString(0x1C, Encoding.ASCII);

            // Read palette
            var paletteSize = GetPaletteDataSize(header.paletteFormat, (TMXPixelFormat)header.imageFormat);
            var paletteData = br.ReadBytes(paletteSize);

            // Read image data
            var dataSize = GetImageDataSize(header.imageFormat, header.width, header.height);
            var imageData = br.ReadBytes(dataSize);

            // Read mip data
            var mips = new List<byte[]>();
            for (var i = 1; i <= header.mipmapCount; i++)
            {
                var mipSize = GetImageDataSize(header.imageFormat, header.width >> i, header.height >> i);
                mips.Add(br.ReadBytes(mipSize));
            }

            // Create image info

            var imageInfo = new ImageFileInfo
            {
                Name = "",
                BitDepth = imageData.Length * 8 / (header.width * header.height),
                ImageData = imageData,
                ImageFormat = header.imageFormat,
                ImageSize = new Size(header.width, header.height),
                MipMapData = header.mipmapCount > 0 ? mips : new List<byte[]>()
            };

            if (paletteData.Length > 0)
            {
                SwizzlePaletteData(paletteData, header.paletteFormat);

                imageInfo.PaletteData = paletteData;
                imageInfo.PaletteFormat = header.paletteFormat;
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
                output.Position = paletteOffset;
                output.Write(imageInfo.PaletteData);
            }

            // Write comment
            output.Position = commentOffset;
            bw.WriteString(_comment, Encoding.ASCII);

            // Write header
            var header = new TmxHeader
            {
                magic = "TMX0",
                imageFormat = (byte)imageInfo.ImageFormat,
                paletteFormat = (byte)(imageInfo.PaletteData != null ? imageInfo.PaletteFormat : 0),
                width = (short)imageInfo.ImageSize.Width,
                height = (short)imageInfo.ImageSize.Height,
                fileSize = (int)output.Length,
                mipmapCount = (byte)imageInfo.MipMapData.Count,
            };

            output.Position = 0;
            WriteHeader(header, bw);
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
            var bitDepth = TmxSupport.ColorFormats.ContainsKey(imageFormat) ?
                TmxSupport.ColorFormats[imageFormat].BitDepth :
                TmxSupport.IndexFormats[imageFormat].BitDepth;

            var dataSize = bitDepth * width * height / 8;

            return dataSize;
        }

        private void SwizzlePaletteData(byte[] palette, int paletteFormat)
        {
            var paletteEncoding = TmxSupport.ColorFormats[paletteFormat];
            var colorDepth = paletteEncoding.BitDepth / 8;

            if (palette.Length <= 16 * colorDepth)
                return;

            for (var i = 0; i < palette.Length; i += colorDepth * 32)
            {
                var rowLength = colorDepth * 8;

                var row1Index = i + rowLength;
                var row2Index = i + rowLength * 2;

                var tmp = new byte[rowLength];
                Array.Copy(palette, row1Index, tmp, 0, rowLength);

                Array.Copy(palette, row2Index, palette, row1Index, rowLength);
                Array.Copy(tmp, 0, palette, row2Index, rowLength);
            }
        }
    }
}
