using System;
using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_nintendo.NW4C;

namespace plugin_nintendo.Images
{
    public class Bxlim
    {
        private static readonly int Nw4CHeaderSize = Tools.MeasureType(typeof(NW4CHeader));
        private static readonly int BclimHeaderSize = Tools.MeasureType(typeof(BclimHeader));
        private static readonly int BflimHeaderSize = Tools.MeasureType(typeof(BflimHeader));

        private NW4CHeader _header;

        private BclimHeader _bclimHeader;
        private BflimHeader _bflimHeader;

        public bool IsCtr { get; private set; }

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input, ByteOrder.BigEndian);

            // Read byte order
            input.Position = input.Length - 0x24;
            var byteOrder = br.ReadType<ByteOrder>();
            br.ByteOrder = byteOrder;

            // Read common header
            input.Position = input.Length - 0x28;
            _header = br.ReadType<NW4CHeader>();

            switch (_header.magic)
            {
                case "CLIM":
                    IsCtr = true;
                    return LoadBclim(br);

                case "FLIM":
                    IsCtr = byteOrder == ByteOrder.LittleEndian;
                    return LoadBflim(br);

                default:
                    throw new InvalidOperationException($"{_header.magic} is not supported.");
            }
        }

        public void Save(Stream output, ImageInfo image)
        {
            using var bw = new BinaryWriterX(output, _header.byteOrder);

            // Calculate offsets
            var nw4cOffset = ((image.ImageData.Length + 0xF) & ~0xF);
            var headerOffset = nw4cOffset + Nw4CHeaderSize;

            // Write image data
            output.Write(image.ImageData);

            // Write NW4C header
            _header.fileSize = headerOffset + 0x8 + (_bclimHeader == null ? BflimHeaderSize : BclimHeaderSize);

            output.Position = nw4cOffset;
            bw.WriteType(_header);

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
                    sectionSize = 0x4 + BclimHeaderSize,
                    sectionData = _bclimHeader
                };

                output.Position = headerOffset;
                bw.WriteType(section);
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
                    sectionSize = 0x4 + BclimHeaderSize,
                    sectionData = _bclimHeader
                };

                output.Position = headerOffset;
                bw.WriteType(section);
            }
        }

        private ImageInfo LoadBclim(BinaryReaderX br)
        {
            // Read section
            var imageSection = br.ReadType<NW4CSection<BclimHeader>>();
            _bclimHeader = imageSection.sectionData;

            // Read image data
            br.BaseStream.Position = 0;
            var imageData = br.ReadBytes(_bclimHeader.dataSize);

            var size = new Size(_bclimHeader.width, _bclimHeader.height);

            // Create image info
            var imageInfo = new ImageInfo(imageData, _bclimHeader.format, size);
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context, _bclimHeader.transformation));
            imageInfo.PadSize.ToPowerOfTwo();

            return imageInfo;
        }

        private ImageInfo LoadBflim(BinaryReaderX br)
        {
            // Read section
            var imageSection = br.ReadType<NW4CSection<BflimHeader>>();
            _bflimHeader = imageSection.sectionData;

            // Read image data
            br.BaseStream.Position = 0;
            var imageData = br.ReadBytes(_bflimHeader.dataSize);

            var size = new Size(_bflimHeader.width, _bflimHeader.height);

            // Create image info
            var imageInfo = new ImageInfo(imageData, _bflimHeader.format, size);
            imageInfo.RemapPixels.With(context => IsCtr
                ? (IImageSwizzle)new CtrSwizzle(context, (CtrTransformation)_bflimHeader.swizzleTileMode)
                : new CafeSwizzle(context, _bflimHeader.swizzleTileMode));

            return imageInfo;
        }
    }
}
