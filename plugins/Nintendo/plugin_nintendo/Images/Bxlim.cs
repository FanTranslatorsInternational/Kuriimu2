using Kanvas.Contract.Enums.Swizzle;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using plugin_nintendo.NW4C;
using SixLabors.ImageSharp;
using static System.Collections.Specialized.BitVector32;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

namespace plugin_nintendo.Images
{
    public class Bxlim
    {
        private const int Nw4CHeaderSize_ = 0x14;
        private const int BclimHeaderSize_ = 0xC;
        private const int BflimHeaderSize_ = 0xC;

        private NW4CHeader _header;
        private ByteOrder _byteOrder;

        private BclimHeader _bclimHeader;
        private BflimHeader _bflimHeader;

        public bool IsCtr { get; private set; }

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input, ByteOrder.BigEndian);

            // Read byte order
            input.Position = input.Length - 0x24;
            _byteOrder = (ByteOrder)br.ReadUInt16();
            br.ByteOrder = _byteOrder;

            // Read common header
            input.Position = input.Length - 0x28;
            _header = ReadNw4cHeader(br);

            switch (_header.magic)
            {
                case "CLIM":
                    IsCtr = true;
                    return LoadBclim(br);

                case "FLIM":
                    IsCtr = _byteOrder == ByteOrder.LittleEndian;
                    return LoadBflim(br);

                default:
                    throw new InvalidOperationException($"{_header.magic} is not supported.");
            }
        }

        public void Save(Stream output, ImageFileInfo image)
        {
            using var bw = new BinaryWriterX(output, _byteOrder);

            // Calculate offsets
            var nw4COffset = (image.ImageData.Length + 0xF) & ~0xF;
            var headerOffset = nw4COffset + Nw4CHeaderSize_;

            // Write image data
            output.Write(image.ImageData);

            // Write NW4C header
            _header.fileSize = headerOffset + 0x8 + (_bclimHeader == null ? BflimHeaderSize_ : BclimHeaderSize_);

            output.Position = nw4COffset;
            WriteNw4cHeader(_header, bw);

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
                WriteNw4cSection(section, bw);
            }
            else
            {
                _bflimHeader.format = (byte)image.ImageFormat;
                _bflimHeader.dataSize = image.ImageData.Length;
                _bflimHeader.width = (short)image.ImageSize.Width;
                _bflimHeader.height = (short)image.ImageSize.Height;

                var section = new NW4CSection<BflimHeader>
                {
                    magic = "imag",
                    sectionSize = 0x4 + BflimHeaderSize_,
                    sectionData = _bflimHeader
                };

                output.Position = headerOffset;
                WriteNw4cSection(section, bw);
            }
        }

        private ImageFileInfo LoadBclim(BinaryReaderX br)
        {
            // Read section
            var imageSection = ReadBclimSection(br);
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

        private ImageFileInfo LoadBflim(BinaryReaderX br)
        {
            // Read section
            var imageSection = ReadBflimSection(br);
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

        private NW4CHeader ReadNw4cHeader(BinaryReaderX reader)
        {
            return new NW4CHeader
            {
                magic = reader.ReadString(4),
                byteOrder = reader.ReadUInt16(),
                headerSize = reader.ReadInt16(),
                version = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                sectionCount = reader.ReadInt16(),
                padding = reader.ReadInt16()
            };
        }

        private NW4CSection<BclimHeader> ReadBclimSection(BinaryReaderX reader)
        {
            return new NW4CSection<BclimHeader>
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                sectionData = new BclimHeader
                {
                    width = reader.ReadInt16(),
                    height = reader.ReadInt16(),
                    format = reader.ReadByte(),
                    transformation = reader.ReadByte(),
                    alignment = reader.ReadInt16(),
                    dataSize = reader.ReadInt32()
                }
            };
        }

        private NW4CSection<BflimHeader> ReadBflimSection(BinaryReaderX reader)
        {
            return new NW4CSection<BflimHeader>
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                sectionData = new BflimHeader
                {
                    width = reader.ReadInt16(),
                    height = reader.ReadInt16(),
                    alignment = reader.ReadInt16(),
                    format = reader.ReadByte(),
                    swizzleTileMode = reader.ReadByte(),
                    dataSize = reader.ReadInt32()
                }
            };
        }

        private void WriteNw4cHeader(NW4CHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.byteOrder);
            writer.Write(header.headerSize);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.sectionCount);
            writer.Write(header.padding);
        }

        private void WriteNw4cSection(NW4CSection<BclimHeader> section, BinaryWriterX writer)
        {
            writer.WriteString(section.magic, writeNullTerminator: false);
            writer.Write(section.sectionSize);

            writer.Write(section.sectionData.width);
            writer.Write(section.sectionData.height);
            writer.Write(section.sectionData.format);
            writer.Write(section.sectionData.transformation);
            writer.Write(section.sectionData.alignment);
            writer.Write(section.sectionData.dataSize);
        }

        private void WriteNw4cSection(NW4CSection<BflimHeader> section, BinaryWriterX writer)
        {
            writer.WriteString(section.magic, writeNullTerminator: false);
            writer.Write(section.sectionSize);

            writer.Write(section.sectionData.width);
            writer.Write(section.sectionData.height);
            writer.Write(section.sectionData.alignment);
            writer.Write(section.sectionData.format);
            writer.Write(section.sectionData.swizzleTileMode);
            writer.Write(section.sectionData.dataSize);
        }
    }
}
