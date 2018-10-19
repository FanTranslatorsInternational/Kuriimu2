using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Swizzle;
using Komponent.IO;

namespace Kore.SamplePlugins
{
    public class BCLIM //: IFormatConverter<>
    {
        public NW4CHeader FileHeader { get; private set; }
        public ImageHeader TextureHeader { get; private set; }
        public ImageSettings Settings { get; set; }

        public Bitmap Texture { get; set; }

        public BCLIM() { }

        public BCLIM(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                var texture = br.ReadBytes((int)br.BaseStream.Length - 0x28);

                FileHeader = br.ReadStruct<NW4CHeader>();
                br.ByteOrder = FileHeader.ByteOrder;
                TextureHeader = br.ReadStruct<ImageHeader>();

                Settings = new ImageSettings
                {
                    Width = TextureHeader.Width,
                    Height = TextureHeader.Height,
                    Format = DDDSFormat[TextureHeader.Format],
                    Swizzle = new CTRSwizzle(TextureHeader.Width, TextureHeader.Height, TextureHeader.SwizzleTileMode)
                };

                Texture = Common.Load(texture, Settings);
            }
        }

        public void Save(Stream output)
        {

        }

        public class NW4CHeader
        {
            [FixedLength(4)]
            public string Magic;
            public ByteOrder ByteOrder;
            public short HeaderSize;
            public int Version;
            public int FileSize;
            public short SectionCount;
            public short Padding;
        }

        public class ImageHeader
        {
            [FixedLength(4)]
            public string Magic;
            public int SectionSize;
            public short Width;
            public short Height;
            public byte Format;
            public byte SwizzleTileMode; // Not used in BCLIM
            public short Alignment;
            public int DataSize;
        }

        public Dictionary<byte, IImageFormat> DDDSFormat = new Dictionary<byte, IImageFormat>
        {
            [0] = new LA(8, 0),
            [1] = new LA(0, 8),
            [2] = new LA(4, 4),
            [3] = new LA(8, 8),
            [4] = new HL(8, 8),
            [5] = new RGBA(5, 6, 5),
            [6] = new RGBA(8, 8, 8),
            [7] = new RGBA(5, 5, 5, 1),
            [8] = new RGBA(4, 4, 4, 4),
            [9] = new RGBA(8, 8, 8, 8),
            //[10] = new ETC1(),
            //[11] = new ETC1(true),
            [18] = new LA(4, 0),
            [19] = new LA(0, 4),
        };

        public Dictionary<byte, IImageFormat> WiiUFormat = new Dictionary<byte, IImageFormat>
        {
            [0] = new LA(8, 0, ByteOrder.BigEndian),
            [1] = new LA(0, 8, ByteOrder.BigEndian),
            [2] = new LA(4, 4, ByteOrder.BigEndian),
            [3] = new LA(8, 8, ByteOrder.BigEndian),
            [4] = new HL(8, 8, ByteOrder.BigEndian),
            [5] = new RGBA(5, 6, 5, 0, false, false, ByteOrder.BigEndian),
            [6] = new RGBA(8, 8, 8, 0, false, false, ByteOrder.BigEndian),
            [7] = new RGBA(5, 5, 5, 1, false, false, ByteOrder.BigEndian),
            [8] = new RGBA(4, 4, 4, 4, false, false, ByteOrder.BigEndian),
            [9] = new RGBA(8, 8, 8, 8, false, false, ByteOrder.BigEndian),
            //[10] = new ETC1(false, false, ByteOrder.BigEndian),
            //[11] = new ETC1(true, false, ByteOrder.BigEndian),
            [12] = new DXT(DXT.Format.DXT1),
            [13] = new DXT(DXT.Format.DXT3),
            [14] = new DXT(DXT.Format.DXT5),
            [15] = new ATI(ATI.Format.ATI1L),
            [16] = new ATI(ATI.Format.ATI1A),
            [17] = new ATI(ATI.Format.ATI2),
            [18] = new LA(4, 0, ByteOrder.BigEndian),
            [19] = new LA(0, 4, ByteOrder.BigEndian),
            [20] = new RGBA(8, 8, 8, 8, false, false, ByteOrder.BigEndian),
            [21] = new DXT(DXT.Format.DXT1),
            [22] = new DXT(DXT.Format.DXT3),
            [23] = new DXT(DXT.Format.DXT5),
            [24] = new RGBA(10, 10, 10, 2, false, false, ByteOrder.BigEndian)
        };
    }
}
