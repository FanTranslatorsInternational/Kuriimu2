using System.Buffers.Binary;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_sting_entertainment.Archives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_sting_entertainment.Images
{
    class Tex
    {
        private static readonly int HeaderSize = 0xC;
        private static readonly int TexHeaderSize = 0x10;

        private TexHeader _texHeader;
        private uint _magic;
        private byte[] _unkRegion;

        public IImageFile Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = PckSupport.ReadHeader(br);
            _texHeader = ReadHeader(br);

            // Read data
            var imgData = new SubStream(input, input.Position, _texHeader.dataSize);

            var buffer = new byte[4];
            _ = imgData.Read(buffer);
            imgData.Position -= 4;

            IImageFile image;
            switch (_magic = BinaryPrimitives.ReadUInt32BigEndian(buffer))
            {
                case 0x89504E47:    // PNG
                    image = new StaticImageFile(Image.Load<Rgba32>(imgData));
                    break;

                case 0x4C5A3737:    // LZ77

                    // Decompress image data buffer
                    var decompressedData = new MemoryStream();
                    Compressions.StingLz.Build().Decompress(imgData, decompressedData);

                    decompressedData.Position = 0;
                    var dataBr = new BinaryReaderX(decompressedData);

                    // Prepare image info
                    var dataSize = _texHeader.width * _texHeader.height;
                    var imageInfo = new ImageFileInfo
                    {
                        BitDepth = 32,
                        ImageData = dataBr.ReadBytes(dataSize),
                        ImageFormat = 0,
                        ImageSize = new Size(_texHeader.width, _texHeader.height),
                        PaletteData = dataBr.ReadBytes(256 * 4),
                        PaletteFormat = 0
                    };

                    image = new ImageFile(imageInfo, TexSupport.GetEncodingDefinition());

                    break;

                default:
                    throw new InvalidOperationException("Unknown data type.");
            }

            // Read unknown region
            input.Position = (header.size + 3) & ~3;
            _unkRegion = br.ReadBytes((int)(input.Length - input.Position));

            return image;
        }

        public void Save(Stream output, IImageFile image)
        {
            using var bw = new BinaryWriterX(output);

            // Prepare image data
            var imgData = new MemoryStream();
            switch (_magic)
            {
                case 0x89504E47:    // PNG
                    image.GetImage().SaveAsPng(imgData);
                    break;

                case 0x4C5A3737:    // LZ77
                    imgData.Write(image.ImageInfo.ImageData);
                    imgData.Write(image.ImageInfo.PaletteData);

                    imgData.Position = 0;
                    var compData = new MemoryStream();
                    Compressions.StingLz.Build().Compress(imgData, compData);

                    imgData = compData;
                    break;
            }

            // Write headers
            PckSupport.WriteHeader(new PckHeader
            {
                magic = "Texture ",
                size = (int)(imgData.Length + HeaderSize + TexHeaderSize)
            }, bw);
            WriteHeader(new TexHeader
            {
                unk1 = _texHeader.unk1, 
                dataSize = (int)imgData.Length, 
                width = image.ImageInfo.ImageSize.Width, 
                height = image.ImageInfo.ImageSize.Height
            }, bw);

            // Write image data
            imgData.Position = 0;
            imgData.CopyTo(output);
            bw.WriteAlignment(4);

            // Write unknown region
            bw.Write(_unkRegion);
        }

        private TexHeader ReadHeader(BinaryReaderX reader)
        {
            return new TexHeader
            {
                unk1 = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32()
            };
        }

        private void WriteHeader(TexHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.dataSize);
            writer.Write(header.width);
            writer.Write(header.height);
        }
    }
}
