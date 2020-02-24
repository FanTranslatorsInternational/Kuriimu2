//using System.Drawing;
//using System.IO;
//using Kanvas;
//using Kanvas.Models;
//using Kanvas.Swizzle;
//using Komponent.IO;
//using plugin_nintendo.NW4C;

//namespace plugin_nintendo.BCLIM
//{
//    public class BCLIM
//    {
//        public NW4CHeader FileHeader { get; private set; }
//        public BclimHeader TextureHeader { get; private set; }
//        public ImageSettings Settings { get; set; }

//        public Bitmap Texture { get; set; }

//        public BCLIM(Stream input)
//        {
//            using (var br = new BinaryReaderX(input))
//            {
//                var texture = br.ReadBytes((int)br.BaseStream.Length - 0x28);

//                FileHeader = br.ReadType<NW4CHeader>();
//                br.ByteOrder = FileHeader.ByteOrder;
//                TextureHeader = br.ReadType<BclimHeader>();

//                Settings = new ImageSettings(ImageFormats.CtrFormats[TextureHeader.Format], TextureHeader.Width, TextureHeader.Height)
//                {
//                    Swizzle = new CTRSwizzle(TextureHeader.Width, TextureHeader.Height, TextureHeader.SwizzleTileMode)
//                };

//                Texture = Common.Load(texture, Settings);
//            }
//        }

//        public void Save(Stream output)
//        {

//        }
//    }
//}
