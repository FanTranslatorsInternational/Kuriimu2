using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kanvas;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_nintendo.NW4R;

namespace plugin_nintendo.Images
{
    class Tex0Header
    {
        public int unk1;
        public short width;
        public short height;
        public int format;
        public int imgCount;
        public int unk2;
        public int mipLevels;   // imgCount - 1
        public int unk3;
    }

    class Plt0Header
    {
        public int format;
        public short colorCount;
        public short zero0;
    }

    class Tex0File
    {
        private ByteOrder _byteOrder;

        public Nw4rCommonHeader CommonHeader { get; }

        public Tex0Header Header { get; }

        public byte[] ImageData { get; set; }

        public IList<byte[]> MipData { get; }

        public Tex0File(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Determine byte order
            input.Position = 4;
            _byteOrder = br.ByteOrder = br.ReadInt32() != input.Length ? ByteOrder.BigEndian : ByteOrder.LittleEndian;

            // Read common header
            input.Position = 0;
            CommonHeader = br.ReadType<Nw4rCommonHeader>();

            // Read tex header
            Header = br.ReadType<Tex0Header>();

            // Read main image data
            var bitDepth = Tex0Support.ColorFormats.ContainsKey(Header.format) ?
                Tex0Support.ColorFormats[Header.format].BitDepth :
                Tex0Support.IndexFormats[Header.format].BitDepth;

            input.Position = CommonHeader.sectionOffsets[0];
            var dataSize = Header.width * Header.height * bitDepth / 8;
            ImageData = br.ReadBytes(dataSize);

            // Read mip level data
            MipData = new List<byte[]>();

            var (width, height) = ((int)Header.width, (int)Header.height);
            for (var i = 0; i < Header.mipLevels; i++)
            {
                (width, height) = (width >> 1, height >> 1);
                dataSize = width * height * bitDepth;

                MipData.Add(br.ReadBytes(dataSize));
            }
        }

        public void Write(Stream input)
        {
            using var bw = new BinaryWriterX(input, _byteOrder);

            // Calculate offsets
            var texHeaderOffset = 0x14 + CommonHeader.sectionOffsets.Length * 4;

            // Write tex header
            input.Position = texHeaderOffset;
            bw.WriteType(Header);

            // Write image data
            input.Position = CommonHeader.sectionOffsets[0];
            bw.Write(ImageData);
            foreach (var mipData in MipData)
                bw.Write(mipData);

            // Write common header
            CommonHeader.size = (int)input.Length;
            CommonHeader.bresOffset = 0;
            CommonHeader.nameOffset = 0;

            input.Position = 0;
            bw.WriteType(CommonHeader);
        }
    }

    class Plt0File
    {
        private ByteOrder _byteOrder;

        public Nw4rCommonHeader CommonHeader { get; }

        public Plt0Header Header { get; }

        public byte[] PaletteData { get; set; }

        public Plt0File()
        {
            CommonHeader = new Nw4rCommonHeader();
            Header = new Plt0Header();
        }

        public Plt0File(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Determine byte order
            input.Position = 4;
            _byteOrder = br.ByteOrder = br.ReadInt32() != input.Length ? ByteOrder.BigEndian : ByteOrder.LittleEndian;

            // Read common header
            input.Position = 0;
            CommonHeader = br.ReadType<Nw4rCommonHeader>();

            // Read plt header
            Header = br.ReadType<Plt0Header>();

            // Read main image data
            var bitDepth = Tex0Support.PaletteFormats[Header.format].BitDepth;

            input.Position = CommonHeader.sectionOffsets[0];
            var dataSize = Header.colorCount * bitDepth / 8;
            PaletteData = br.ReadBytes(dataSize);
        }

        public void Write(Stream input)
        {
            using var bw = new BinaryWriterX(input, _byteOrder);

            // Calculate offsets
            var pltHeaderOffset = 0x14 + CommonHeader.sectionOffsets.Length * 4;

            // Write PLT header
            input.Position = pltHeaderOffset;
            bw.WriteType(Header);

            // Write image data
            input.Position = CommonHeader.sectionOffsets[0];
            bw.Write(PaletteData);

            // Write common header
            CommonHeader.size = (int)input.Length;
            CommonHeader.bresOffset = 0;
            CommonHeader.nameOffset = 0;

            input.Position = 0;
            bw.WriteType(CommonHeader);
        }
    }

    class Tex0Support
    {
        public static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Wii.L4(),
            [0x01] = ImageFormats.Wii.L8(),
            [0x02] = ImageFormats.Wii.La44(),
            [0x03] = ImageFormats.Wii.La88(),
            [0x04] = ImageFormats.Wii.Rgb565(),
            [0x05] = ImageFormats.Wii.Rgb5A3(),
            [0x06] = ImageFormats.Wii.Rgba8888(),

            [0x0E] = ImageFormats.Wii.Cmpr()
        };

        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0x08] = ImageFormats.Wii.I4(),
            [0x09] = ImageFormats.Wii.I8(),
            [0x0A] = ImageFormats.Wii.I14()
        };

        public static readonly IDictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Wii.La88(),
            [0x01] = ImageFormats.Wii.Rgb565(),
            [0x02] = ImageFormats.Wii.Rgb5A3()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddColorEncodings(ColorFormats);

            definition.AddPaletteEncodings(PaletteFormats);
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition(x.Value, new List<int> { 0, 1, 2 }))).ToArray());

            return definition;
        }
    }
}
