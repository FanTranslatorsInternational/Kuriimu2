using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Images
{
    public class MtexHeader
    {
        [FixedLength(4)]        
        public string Magic = "XETM";
        public int Unk1;
        public int Unk2;
        public short Unk3;
        public short Width;
        public short Height;
        public short Unk4;
        public short Format;        
    }

    public static class MtexSupport
    {
        public enum MtexImageFormat : short
        {
            ETC1 = 0x00,
            ETC1A4 = 0x01
        }

        public static IDictionary<int, IColorEncoding> MtexFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = new Etc1(false, true, ByteOrder.LittleEndian),
            [0x01] = new Etc1(true, true, ByteOrder.LittleEndian),
        };
    }
}
