using Kanvas.Contract.Enums.Swizzle;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using plugin_nintendo.NW4C;
using SixLabors.ImageSharp;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

namespace plugin_nintendo.Images
{
    public class Bxlim
    {
        private const int Nw4CHeaderSize_ = 0x14;
        private const int BclimHeaderSize_ = 0xC;
        private const int BflimHeaderSize_ = 0xC;

        private NW4CHeader _header;

        private BclimHeader _bclimHeader;
        private BflimHeader _bflimHeader;

        public bool IsCtr { get; private set; }

        public ImageFileInfo Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, ByteOrder.BigEndian);

            // Read byte order
            input.Position = input.Length - 0x24;
            var byteOrder = (ByteOrder)br.ReadUInt16();
            br.ByteOrder = byteOrder;

            // Read common header
            input.Position = input.Length - 0x28;
            _header = typeReader.Read<NW4CHeader>(br);

            switch (_header.magic)
            {
                case "CLIM":
                    IsCtr = true;
                    return LoadBclim(typeReader, br);

                case "FLIM":
                    IsCtr = byteOrder == ByteOrder.LittleEndian;
                    return LoadBflim(typeReader, br);

                default:
                    throw new InvalidOperationException($"{_header.magic} is not supported.");
            }
        }

        public void Save(Stream output, ImageFileInfo image)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output, (ByteOrder)_header.byteOrder);

            // Calculate offsets
            var nw4COffset = ((image.ImageData.Length + 0xF) & ~0xF);
            var headerOffset = nw4COffset + Nw4CHeaderSize_;

            // Write image data
            output.Write(image.ImageData);

            // Write NW4C header
            _header.fileSize = headerOffset + 0x8 + (_bclimHeader == null ? BflimHeaderSize_ : BclimHeaderSize_);

            output.Position = nw4COffset;
            typeWriter.Write(_header, bw);

            // Write img header
            if (_bclimHeader != null)
            {
                _bclimHeader.format = (byte)image.ImageFormat;
                _bclimHeader.dataSize = image.ImageData.Length;
                _bclimHeader.width = (short)image.ImageSize.Width;
                _bclimHeader.height = (short)image.ImageSize.Height;

                var section = new NW4CSection<BclimHeader>
                {
                    magic = "imag",
                    sectionSize = 0x4 + BclimHeaderSize_,
                    sectionData = _bclimHeader
                };

                output.Position = headerOffset;
                typeWriter.Write(section, bw);
            }
            else
            {
                _bflimHeader.format = (byte)image.ImageFormat;
                _bflimHeader.dataSize = image.ImageData.Length;
                _bflimHeader.width = (short)image.ImageSize.Width;
                _bflimHeader.height = (short)image.ImageSize.Height;

                var section = new NW4CSection<BclimHeader>
                {
                    magic = "imag",
                    sectionSize = 0x4 + BclimHeaderSize_,
                    sectionData = _bclimHeader
                };

                output.Position = headerOffset;
                typeWriter.Write(section, bw);
            }
        }

        private ImageFileInfo LoadBclim(BinaryTypeReader typeReader, BinaryReaderX br)
        {
            // Read section
            var imageSection = typeReader.Read<NW4CSection<BclimHeader>>(br);
            _bclimHeader = imageSection.sectionData;

            // Read image data
            br.BaseStream.Position = 0;
            var imageData = br.ReadBytes(_bclimHeader.dataSize);

            var size = new Size(_bclimHeader.width, _bclimHeader.height);

            // Create image info
            var encodingDefinition = IsCtr ? BxlimSupport.GetCtrDefinition() : BxlimSupport.GetCafeDefinition();
            var imageInfo = new ImageFileInfo
            {
                BitDepth = encodingDefinition.GetColorEncoding(_bclimHeader.format).BitDepth,
                ImageData = imageData,
                ImageFormat = _bclimHeader.format,
                ImageSize = size,
                RemapPixels = context => new CtrSwizzle(context, (CtrTransformation)_bclimHeader.transformation),
                PadSize = builder => builder.ToPowerOfTwo()
            };

            return imageInfo;
        }

        private ImageFileInfo LoadBflim(BinaryTypeReader typeReader, BinaryReaderX br)
        {
            // Read section
            var imageSection = typeReader.Read<NW4CSection<BflimHeader>>(br);
            _bflimHeader = imageSection.sectionData;

            // Read image data
            br.BaseStream.Position = 0;
            var imageData = br.ReadBytes(_bflimHeader.dataSize);

            var size = new Size(_bflimHeader.width, _bflimHeader.height);

            // Create image info
            var encodingDefinition = IsCtr ? BxlimSupport.GetCtrDefinition() : BxlimSupport.GetCafeDefinition();
            var imageInfo = new ImageFileInfo
            {
                BitDepth = encodingDefinition.GetColorEncoding(_bflimHeader.format).BitDepth,
                ImageData = imageData,
                ImageFormat = _bflimHeader.format,
                ImageSize = size,
                RemapPixels = context => IsCtr
                    ? new CtrSwizzle(context, (CtrTransformation)_bflimHeader.swizzleTileMode)
                    : new CafeSwizzle(context, _bflimHeader.swizzleTileMode),
                PadSize = IsCtr ? builder => builder.ToPowerOfTwo() : null
            };

            return imageInfo;
        }
    }
}
