using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_atlus.Images
{
    /* Original understanding by xdaniel and his tool Tharsis
     * https://github.com/xdanieldzd/Tharsis */

    class Tmx
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(TmxHeader));

        private TmxHeader _header;
        private string _comment;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<TmxHeader>();
            _comment = br.ReadString(0x1C, Encoding.ASCII);

            // Read palette
            var paletteSize = GetPaletteDataSize((int)_header.paletteFormat, _header.imageFormat);
            var paletteData = br.ReadBytes(paletteSize);

            // Read image data
            var dataSize = GetImageDataSize((int)_header.imageFormat, _header.width, _header.height);
            var imageData = br.ReadBytes(dataSize);

            // Read mip data
            var mips = new List<byte[]>();
            for (var i = 1; i <= _header.mipmapCount; i++)
            {
                var mipSize = GetImageDataSize((int)_header.imageFormat, _header.width >> i, _header.height >> i);
                mips.Add(br.ReadBytes(mipSize));
            }

            // Create image info
            var imageInfo = new ImageInfo(imageData, (int)_header.imageFormat, new Size(_header.width, _header.height));

            if (_header.mipmapCount > 0)
                imageInfo.MipMapData = mips;

            if (paletteData.Length > 0)
            {
                SwizzlePaletteData(paletteData, (int)_header.paletteFormat);

                imageInfo.PaletteData = paletteData;
                imageInfo.PaletteFormat = (int)_header.paletteFormat;
            }

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var commentOffset = HeaderSize;
            var paletteOffset = commentOffset + 0x1C;
            var dataOffset = paletteOffset + (imageInfo.HasPaletteInformation ? GetPaletteDataSize((int)_header.paletteFormat, _header.imageFormat) : 0);

            // Write image data
            output.Position = dataOffset;
            output.Write(imageInfo.ImageData);

            if (imageInfo.MipMapCount > 0)
                foreach (var mipData in imageInfo.MipMapData)
                    output.Write(mipData);

            // Write palette data
            if (imageInfo.HasPaletteInformation)
            {
                output.Position = paletteOffset;
                output.Write(imageInfo.PaletteData);
            }

            // Write comment
            output.Position = commentOffset;
            bw.WriteString(_comment, Encoding.ASCII, false);

            // Write header
            _header.imageFormat = (TMXPixelFormat)imageInfo.ImageFormat;
            _header.paletteFormat = (TMXPixelFormat)(imageInfo.HasPaletteInformation ? imageInfo.PaletteFormat : 0);
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.fileSize = (int)output.Length;
            _header.mipmapCount = (byte)imageInfo.MipMapCount;

            output.Position = 0;
            bw.WriteType(_header);
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
            var bitDepth = TmxSupport.ColorFormats.ContainsKey((int)_header.imageFormat) ?
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
