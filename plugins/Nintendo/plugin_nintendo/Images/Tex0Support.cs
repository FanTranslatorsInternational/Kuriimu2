using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Kanvas.Encoding.PlatformSpecific.Wii;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
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

        public int BitDepth { get; set; }

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
            CommonHeader = ReadNw4rCommonHeader(br);

            // Read tex header
            Header = ReadTexHeader(br);

            // Read main image data
            BitDepth = Tex0Support.ColorFormats.TryGetValue(Header.format, out var format) ?
                format.BitDepth :
                Tex0Support.IndexFormats[Header.format].BitDepth;

            input.Position = CommonHeader.sectionOffsets[0];
            var dataSize = Header.width * Header.height * BitDepth / 8;
            ImageData = br.ReadBytes(dataSize);

            // Read mip level data
            MipData = new List<byte[]>();

            var (width, height) = ((int)Header.width, (int)Header.height);
            for (var i = 0; i < Header.mipLevels; i++)
            {
                (width, height) = (width >> 1, height >> 1);
                dataSize = width * height * BitDepth;

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
            WriteHeader(Header, bw);

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
            WriteNw4rCommonHeader(CommonHeader, bw);
        }

        private Nw4rCommonHeader ReadNw4rCommonHeader(BinaryReaderX br)
        {
            string magic = br.ReadString(4);
            int size = br.ReadInt32();
            int version = br.ReadInt32();
            int bresOffset = br.ReadInt32();

            var sectionOffsets = new int[GetSectionOffsetCount(magic, version)];
            for (var i = 0; i < sectionOffsets.Length; i++)
                sectionOffsets[i] = br.ReadInt32();

            int nameOffset = br.ReadInt32();

            return new Nw4rCommonHeader
            {
                magic = magic,
                size = size,
                version = version,
                bresOffset = bresOffset,
                sectionOffsets = sectionOffsets,
                nameOffset = nameOffset
            };
        }

        private Tex0Header ReadTexHeader(BinaryReaderX reader)
        {
            return new Tex0Header
            {
                unk1 = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                format = reader.ReadInt32(),
                imgCount = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                mipLevels = reader.ReadInt32(),
                unk3 = reader.ReadInt32()
            };
        }

        private void WriteNw4rCommonHeader(Nw4rCommonHeader header, BinaryWriterX bw)
        {
            bw.WriteString(header.magic, writeNullTerminator: false);
            bw.Write(header.size);
            bw.Write(header.version);
            bw.Write(header.bresOffset);

            foreach (int sectionOffset in header.sectionOffsets)
                bw.Write(sectionOffset);

            bw.Write(header.nameOffset);
        }

        private int GetSectionOffsetCount(string magic, int version)
        {
            switch (magic)
            {
                case "TEX0":
                    if (version is 2)
                        return 2;

                    return 1;

                case "PLT0":
                    return 1;

                default:
                    return 0;
            }
        }

        private void WriteHeader(Tex0Header header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.format);
            writer.Write(header.imgCount);
            writer.Write(header.unk2);
            writer.Write(header.mipLevels);
            writer.Write(header.unk3);
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
            CommonHeader = ReadNw4rCommonHeader(br);

            // Read plt header
            Header = ReadPltHeader(br);

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
            WritePltHeader(Header, bw);

            // Write image data
            input.Position = CommonHeader.sectionOffsets[0];
            bw.Write(PaletteData);

            // Write common header
            CommonHeader.size = (int)input.Length;
            CommonHeader.bresOffset = 0;
            CommonHeader.nameOffset = 0;

            input.Position = 0;
            WriteNw4rCommonHeader(CommonHeader, bw);
        }

        private Nw4rCommonHeader ReadNw4rCommonHeader(BinaryReaderX br)
        {
            string magic = br.ReadString(4);
            int size = br.ReadInt32();
            int version = br.ReadInt32();
            int bresOffset = br.ReadInt32();

            var sectionOffsets = new int[GetSectionOffsetCount(magic, version)];
            for (var i = 0; i < sectionOffsets.Length; i++)
                sectionOffsets[i] = br.ReadInt32();

            int nameOffset = br.ReadInt32();

            return new Nw4rCommonHeader
            {
                magic = magic,
                size = size,
                version = version,
                bresOffset = bresOffset,
                sectionOffsets = sectionOffsets,
                nameOffset = nameOffset
            };
        }

        private Plt0Header ReadPltHeader(BinaryReaderX reader)
        {
            return new Plt0Header
            {
                format = reader.ReadInt32(),
                colorCount = reader.ReadInt16(),
                zero0 = reader.ReadInt16()
            };
        }

        private void WriteNw4rCommonHeader(Nw4rCommonHeader header, BinaryWriterX bw)
        {
            bw.WriteString(header.magic, writeNullTerminator: false);
            bw.Write(header.size);
            bw.Write(header.version);
            bw.Write(header.bresOffset);

            foreach (int sectionOffset in header.sectionOffsets)
                bw.Write(sectionOffset);

            bw.Write(header.nameOffset);
        }

        private void WritePltHeader(Plt0Header header, BinaryWriterX writer)
        {
            writer.Write(header.format);
            writer.Write(header.colorCount);
            writer.Write(header.zero0);
        }

        private int GetSectionOffsetCount(string magic, int version)
        {
            switch (magic)
            {
                case "TEX0":
                    if (version is 2)
                        return 2;

                    return 1;

                case "PLT0":
                    return 1;

                default:
                    return 0;
            }
        }
    }

    class Tex0Support
    {
        public static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.L4(),
            [0x01] = ImageFormats.L8(),
            [0x02] = ImageFormats.La44(),
            [0x03] = ImageFormats.La88(),
            [0x04] = ImageFormats.Rgb565(),
            [0x05] = new Rgb5A3(),
            [0x06] = ImageFormats.Rgba8888(),

            [0x0E] = new Bc(BcFormat.Bc1)
        };

        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0x08] = ImageFormats.I4(),
            [0x09] = ImageFormats.I8(),
            [0x0A] = new Kanvas.Encoding.Index(14, ByteOrder.BigEndian)
        };

        public static readonly IDictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.La88(),
            [0x01] = ImageFormats.Rgb565(),
            [0x02] = new Rgb5A3()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddColorEncodings(ColorFormats);

            definition.AddPaletteEncodings(PaletteFormats);
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition
            {
                IndexEncoding = x.Value,
                PaletteEncodingIndices = [0, 1, 2]
            })).ToArray());

            return definition;
        }
    }
}
