using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.Plugins.State.Image;

namespace plugin_level5._3DS.Images
{
    class ZtexHeader
    {
        [FixedLength(4)]
        public string magic;

        public short imageCount;
        public short flags;

        public bool HasExtendedEntries => (flags & 1) != 0;

        public bool HasUnknownEntries => (flags & 2) != 0;
    }

    class ZtexEntry
    {
        [FixedLength(0x40)]
        public string name;
        public uint crc32;
        public int offset;
        public int zero1;
        public int dataSize;
        public short width;
        public short height;
        public byte mipCount;
        public byte format;
        public short unk3 = 0xFF;
    }

    class ZtexUnkEnrty
    {
        public int unk0;
        public int zero0;
    }

    class ZtexSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = ImageFormats.Rgb565(),
            [0x03] = ImageFormats.Rgb888(),
            [0x05] = ImageFormats.Rgba4444(),
            [0x07] = ImageFormats.Rgba5551(),
            [0x09] = ImageFormats.Rgba8888(),
            [0x0B] = ImageFormats.L4(),
            [0x0D] = ImageFormats.Al44(),
            [0x11] = ImageFormats.A8(),
            [0x13] = ImageFormats.L8(),
            [0x15] = ImageFormats.La88(),
            [0x18] = ImageFormats.Etc1(true),
            [0x19] = ImageFormats.Etc1A4(true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
