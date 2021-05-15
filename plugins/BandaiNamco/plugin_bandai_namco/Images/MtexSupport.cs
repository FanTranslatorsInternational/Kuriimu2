using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
#pragma warning disable 649

namespace plugin_bandai_namco.Images
{
    public class MtexHeader
    {
        [FixedLength(4)]        
        public string magic = "XETM";
        public int unk1;
        public int unk2;
        public short unk3;
        public short width;
        public short height;
        public short unk4;
        public short format;        
    }

    public static class MtexSupport
    {
        public static IDictionary<int, IColorEncoding> MtexFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = new Etc1(false, true, ByteOrder.LittleEndian),
            [0x01] = new Etc1(true, true, ByteOrder.LittleEndian),
        };
    }
}
