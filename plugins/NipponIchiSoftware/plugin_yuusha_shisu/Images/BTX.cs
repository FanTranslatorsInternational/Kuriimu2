using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_yuusha_shisu.Images
{
    public class BTX
    {
        private const int HeaderSize = 0x30;

        private BtxHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<BtxHeader>();

            // Read name
            input.Position = _header.nameOffset;
            var name = br.ReadCStringASCII();

            // Read image data
            var dataLength = _header.width * _header.height * BtxSupport.GetBitDepth(_header.format) / 8;
            input.Position = _header.dataOffset;
            var imgData = br.ReadBytes(dataLength);

            // Read mip levels
            IList<byte[]> mips = new List<byte[]>();
            for (var i = 0; i < _header.mipLevels; i++)
            {
                dataLength = (_header.width << (i + 1)) * (_header.height << (i + 1)) * BtxSupport.GetBitDepth(_header.format) / 8;
                mips.Add(br.ReadBytes(dataLength));
            }

            // Read palette data
            var paletteLength = Math.Max(0, (int)input.Length - _header.paletteOffset);
            input.Position = _header.paletteOffset;
            var paletteData = br.ReadBytes(paletteLength);

            // Create image info
            var imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height))
            {
                Name = name
            };

            if (paletteLength > 0)
            {
                imageInfo.PaletteData = paletteData;
                imageInfo.PaletteFormat = 0;
            }

            if (_header.mipLevels > 0)
                imageInfo.MipMapData = mips;

            switch (_header.swizzleMode)
            {
                case 1:
                    imageInfo.RemapPixels.With(context => new VitaSwizzle(context));
                    break;
            }

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffset = HeaderSize;
            var dataOffset = (nameOffset + imageInfo.Name.Length + 0x10) & ~0xF;    // 0x10 = 0x1 + 0xF
            var paletteOffset = (dataOffset + imageInfo.ImageData.Length + (imageInfo.MipMapData?.Sum(x => x.Length) ?? 0) + 0x3F) & ~0x3F;

            // Write name
            output.Position = nameOffset;
            bw.WriteString(imageInfo.Name, Encoding.ASCII, false);

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Write mip levels
            foreach (var mipData in imageInfo.MipMapData)
                bw.Write(mipData);

            // Write palette data
            if (imageInfo.HasPaletteInformation)
            {
                output.Position = paletteOffset;
                bw.Write(imageInfo.PaletteData);
            }

            // Update header
            _header.nameOffset = nameOffset;
            _header.dataOffset = dataOffset;
            _header.paletteOffset = paletteOffset;
            _header.mipLevels = (byte)imageInfo.MipMapCount;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
