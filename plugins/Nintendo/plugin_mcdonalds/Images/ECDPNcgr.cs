using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_mcdonalds.Images
{
    class ECDPNcgr
    {
        private NitroHeader _ncgrHeader;
        private NitroHeader _nclrHeader;
        private NitroCharHeader _charHeader;
        private NitroTtlpHeader _ttlpHeader;

        public ImageFileInfo Load(Stream ncgrStream, Stream nclrStream)
        {
            using var ncgrBr = new BinaryReaderX(ncgrStream);
            using var nclrBr = new BinaryReaderX(nclrStream);

            // Read generic headers
            _ncgrHeader = ReadNitroHeader(ncgrBr);
            _nclrHeader = ReadNitroHeader(nclrBr);

            // Read Char header
            _charHeader = ReadCharHeader(ncgrBr);

            // Read Ttlp header
            _ttlpHeader = ReadTtlpHeader(nclrBr);

            // Read palette data
            int bitDepth = GetBitDepth(_charHeader.imageFormat);
            byte[] paletteData = nclrBr.ReadBytes(_ttlpHeader.paletteSize);

            //byte[] paletteData;
            //if (bitDepth == 4)
            //{
            //    nclrBr.BaseStream.Position += 0xE * 0x20;
            //    paletteData = nclrBr.ReadBytes(0x20);

            //    // Backup complete palette data
            //    nclrBr.BaseStream.Position -= 0xF * 20;
            //    paletteData = nclrBr.ReadBytes(_ttlpHeader.paletteSize);
            //}
            //else
            //    paletteData = nclrBr.ReadBytes(_ttlpHeader.paletteSize);

            // Create image
            var data = ncgrBr.ReadBytes(_charHeader.tileDataSize);
            var size = GetImageSize(_charHeader);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = bitDepth,

                ImageData = data,
                ImageFormat = _charHeader.imageFormat,
                ImageSize = size,

                PaletteData = paletteData,
                PaletteFormat = 0,

                RemapPixels = context => new NitroSwizzle(context)
            };

            return imageInfo;
        }

        // Main logic taken from Tinke "Ekona/Images/Actions.cs Get_Size"
        private Size GetImageSize(NitroCharHeader header)
        {
            if (header.tileCountX > 0)
                return new Size(header.tileCountX * 8, header.tileCountY * 8);

            var pixelCount = header.tileDataSize * 8 / GetBitDepth(header.imageFormat);

            // If image is squared
            var sqrt = (int)Math.Sqrt(pixelCount);
            if ((int)Math.Pow(sqrt, 2) == pixelCount)
                return new Size(sqrt, sqrt);

            // Otherwise derive it from data size
            var width = Math.Min(pixelCount, 0x100);
            width = width == 0 ? 1 : width;

            var height = pixelCount / width;
            height = height == 0 ? 1 : height;

            return new Size(width, height);
        }

        private int GetBitDepth(int format)
        {
            switch (format)
            {
                case 3:
                    return 4;

                case 4:
                    return 8;

                default:
                    throw new InvalidOperationException($"Unsupported image format '{format}'.");
            }
        }

        private NitroHeader ReadNitroHeader(BinaryReaderX reader)
        {
            return new NitroHeader
            {
                magic = reader.ReadString(4),
                byteOrder = reader.ReadUInt16(),
                unk1 = reader.ReadInt16(),
                sectionSize = reader.ReadInt32(),
                headerSize = reader.ReadInt16(),
                sectionCount = reader.ReadInt16()
            };
        }

        private NitroCharHeader ReadCharHeader(BinaryReaderX reader)
        {
            return new NitroCharHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                tileCountX = reader.ReadInt16(),
                tileCountY = reader.ReadInt16(),
                imageFormat = reader.ReadInt32(),
                unk1 = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                tiledFlag = reader.ReadInt32(),
                tileDataSize = reader.ReadInt32(),
                unk3 = reader.ReadInt32()
            };
        }

        private NitroTtlpHeader ReadTtlpHeader(BinaryReaderX reader)
        {
            return new NitroTtlpHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                colorDepth = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                paletteSize = reader.ReadInt32(),
                colorsPerPalette = reader.ReadInt32()
            };
        }
    }
}
