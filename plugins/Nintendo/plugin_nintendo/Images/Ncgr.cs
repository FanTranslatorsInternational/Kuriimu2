using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using plugin_nintendo.Nitro;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Ncgr
    {
        private const int CharHeaderSize_ = 0x20;
        private const int TtlpHeaderSize_ = 0x18;

        private NitroHeader _ncgrHeader;
        private NitroHeader _nclrHeader;
        private NitroCharHeader _charHeader;
        private NitroTtlpHeader _ttlpHeader;

        public ImageFileInfo Load(Stream ncgrStream, Stream nclrStream)
        {
            var typeReader = new BinaryTypeReader();
            using var ncgrBr = new BinaryReaderX(ncgrStream);
            using var nclrBr = new BinaryReaderX(nclrStream);

            // Read generic headers
            _ncgrHeader = typeReader.Read<NitroHeader>(ncgrBr);
            _nclrHeader = typeReader.Read<NitroHeader>(nclrBr);

            // Read Char header
            _charHeader = typeReader.Read<NitroCharHeader>(ncgrBr);

            // Read Ttlp header
            _ttlpHeader = typeReader.Read<NitroTtlpHeader>(nclrBr);

            // Read palette data
            var paletteData = nclrBr.ReadBytes(_ttlpHeader.paletteSize);

            // Create image
            var dataLength = _charHeader.tileCountX < 0 ? _charHeader.tileDataSize : _charHeader.tileCountX * _charHeader.tileCountY;
            var data = ncgrBr.ReadBytes(dataLength);
            var size = GetImageSize(_charHeader);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = GetBitDepth(_charHeader.imageFormat),
                ImageData = data,
                ImageFormat = _charHeader.imageFormat,
                ImageSize = size,
                PaletteData = paletteData,
                PaletteFormat = 0,
                PaletteBitDepth = 15,
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
    }
}
