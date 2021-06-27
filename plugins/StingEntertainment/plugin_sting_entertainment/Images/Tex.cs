using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Kanvas;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Kanvas;
using Kontract.Models.Image;
using plugin_sting_entertainment.Archives;

namespace plugin_sting_entertainment.Images
{
    class Tex
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PckHeader));
        private static readonly int TexHeaderSize = Tools.MeasureType(typeof(TexHeader));

        private TexHeader _texHeader;
        private uint _magic;
        private byte[] _unkRegion;

        public IKanvasImage Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<PckHeader>();
            _texHeader = br.ReadType<TexHeader>();

            // Read data
            var imgData = new SubStream(input, input.Position, _texHeader.dataSize);

            var buffer = new byte[4];
            imgData.Read(buffer);
            imgData.Position -= 4;

            IKanvasImage image;
            switch (_magic = BinaryPrimitives.ReadUInt32BigEndian(buffer))
            {
                case 0x89504E47:    // PNG
                    var img = (Bitmap)Image.FromStream(imgData);
                    image = new BitmapKanvasImage(img);

                    break;

                case 0x4C5A3737:    // LZ77

                    // Decompress image data buffer
                    var decompressedData = new MemoryStream();
                    Compressions.StingLz.Build().Decompress(imgData, decompressedData);

                    decompressedData.Position = 0;
                    var dataBr = new BinaryReaderX(decompressedData);

                    // Prepare image info
                    var dataSize = _texHeader.width * _texHeader.height;
                    var imageInfo = new ImageInfo(dataBr.ReadBytes(dataSize), 0, new Size(_texHeader.width, _texHeader.height))
                    {
                        PaletteData = dataBr.ReadBytes(256 * 4),
                        PaletteFormat = 0
                    };

                    image = new KanvasImage(TexSupport.GetEncodingDefinition(), imageInfo);

                    break;

                default:
                    throw new InvalidOperationException("Unknown data type.");
            }

            // Read unknown region
            input.Position = (header.size + 3) & ~3;
            _unkRegion = br.ReadBytes((int)(input.Length - input.Position));

            return image;
        }

        public void Save(Stream output, IKanvasImage image)
        {
            using var bw = new BinaryWriterX(output);

            // Prepare image data
            var imgData = new MemoryStream();
            switch (_magic)
            {
                case 0x89504E47:    // PNG
                    ((BitmapKanvasImage)image).GetImage().Save(imgData, ImageFormat.Png);
                    break;

                case 0x4C5A3737:    // LZ77
                    imgData.Write(((KanvasImage)image).ImageInfo.ImageData);
                    imgData.Write(((KanvasImage)image).ImageInfo.PaletteData);

                    imgData.Position = 0;
                    var compData = new MemoryStream();
                    Compressions.StingLz.Build().Compress(imgData, compData);

                    imgData = compData;
                    break;
            }

            // Write headers
            bw.WriteType(new PckHeader { magic = "Texture ", size = (int)(imgData.Length + HeaderSize + TexHeaderSize) });
            bw.WriteType(new TexHeader { unk1 = _texHeader.unk1, dataSize = (int)imgData.Length, width = image.ImageSize.Width, height = image.ImageSize.Height });

            // Write image data
            imgData.Position = 0;
            imgData.CopyTo(output);
            bw.WriteAlignment(4);

            // Write unknown region
            bw.Write(_unkRegion);
        }
    }
}
