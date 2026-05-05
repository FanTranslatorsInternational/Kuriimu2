using System.Buffers.Binary;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding.Descriptors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.PlatformSpecific.Wii
{
    public class Rgb5A3(ByteOrder byteOrder = ByteOrder.BigEndian) : IColorEncoding
    {
        private readonly RgbaPixelDescriptor _desc1 = new("ARGB", 4, 4, 4, 3);
        private readonly RgbaPixelDescriptor _desc2 = new("RGB", 5, 5, 5, 0);

        public int BitDepth => 16;
        public ColorChannelBitDepths ColorChannelBitDepths => new(5, 5, 5, 3);
        public int BitsPerValue => 16;
        public int ColorsPerValue => 1;
        public string FormatName => "RGB5A3_Wii";

        public IEnumerable<Rgba32> Load(byte[] input, EncodingOptions options)
        {
            for (var i = 0; i < input.Length; i += 2)
            {
                var value = ReadValue(input, i);
                if ((value & 0x80000000) == 0) yield return _desc1.GetColor(value);
                else yield return _desc2.GetColor(value);
            }
        }

        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions options)
        {
            var buffer = new byte[options.Size.Width * options.Size.Height * 2];

            var offset = 0;
            foreach (var color in colors)
            {
                if (color.A < 0xFF)
                    WriteValue(buffer, offset, (ushort)_desc1.GetValue(color));
                else
                    WriteValue(buffer, offset, (ushort)(_desc2.GetValue(color) | 0x80000000));

                offset += 2;
            }

            return buffer;
        }

        private long ReadValue(byte[] input, int offset)
        {
            return byteOrder == ByteOrder.BigEndian ?
                BinaryPrimitives.ReadUInt16BigEndian(input.AsSpan(offset, 2)) :
                BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(offset, 2));
        }

        private void WriteValue(byte[] input, int offset, ushort value)
        {
            if (byteOrder == ByteOrder.BigEndian)
                BinaryPrimitives.WriteUInt16BigEndian(input.AsSpan(offset, 2), value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(input.AsSpan(offset, 2), value);
        }
    }
}
