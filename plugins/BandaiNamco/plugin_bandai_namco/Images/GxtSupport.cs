using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Images
{
    public class GxtHeader
    {
        [FixedLength(4)]
        public string magic = "GXT\0";
        public int unk1;
        public int unk2;
        public int unk3;
        public int dataSize;
        public int unk4;
        public int unk5;
        public int unk6;
        public int unk7;
        public int imageDataSize;
        public int unk8;
        public int unk9;
        public int unk10;
        public int unk11;
        public short width;
        public short height;
        public int unk12;
    }

    public static class GxtSupport
    {
        public enum GxtImageFormat : short
        {
            RGBA8888 = 0x00,
            Palette_8 = 0x05
        }

        public static IDictionary<int, IColorEncoding> GxtFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = new Rgba(8, 8, 8, 8, ByteOrder.BigEndian),
        };

        public static IDictionary<int, IndexEncodingDefinition> GxtIndexFormats = new Dictionary<int, IndexEncodingDefinition>
        {
            [0x05] = new IndexEncodingDefinition(new Index(8), new[] { 0 })
        };
    }
}
