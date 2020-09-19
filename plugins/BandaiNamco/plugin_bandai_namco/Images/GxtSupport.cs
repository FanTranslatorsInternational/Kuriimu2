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
        public string Magic = "GXT\0";
        public int Unk1;
        public int Unk2;
        public int Unk3;
        public int DataSize;
        public int Unk4;
        public int Unk5;
        public int Unk6;
        public int Unk7;
        public int ImageDataSize;
        public int Unk8;
        public int Unk9;
        public int Unk10;
        public int Unk11;
        public short Width;
        public short Height;
        public int Unk12;
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
