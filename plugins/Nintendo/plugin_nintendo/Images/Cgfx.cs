//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.IO;
//using Kanvas.Swizzle;
//using Komponent.IO;
//using Kontract.Models.Image;
//using Kontract.Models.IO;

//namespace plugin_nintendo.Images
//{
//    class Cgfx
//    {
//        public IList<ImageInfo> Load(Stream input)
//        {
//            using var br = new BinaryReaderX(input);

//            // Determine byte order
//            input.Position = 4;
//            br.ByteOrder = ByteOrder.BigEndian;
//            var byteOrder = br.ReadType<ByteOrder>();
//            br.ByteOrder = byteOrder;

//            // Read header
//            input.Position = 0;
//            var header = br.ReadType<CgfxHeader>();

//            // Read data section
//            var data = CgfxData.Read(br);

//            // Parse texture objects
//            if (data.Data[1] == null)
//                throw new InvalidOperationException("The file contains no images.");

//            var result = new List<ImageInfo>();
//            foreach (var node in data.Data[1].Nodes)
//            {
//                //// Parse TXOB
//                //using var nodeStream = new MemoryStream(node.Data);
//                //using var nodeBr = new BinaryReaderX(nodeStream, br.ByteOrder);
//                //var txob = Txob.Read(nodeBr);

//                //// Prepare data
//                //input.Position = node.nodeDataOffset + 0x48 + txob.Data2.texDataOffset;
//                //var imageData = br.ReadBytes(txob.Data2.texDataSize);

//                //// Create image info
//                //var imageFormat = (txob.Data1.openGLType << 16) | txob.Data1.openGLFormat;
//                //var imageInfo = new ImageInfo(imageData, imageFormat, new Size(txob.Data1.width, txob.Data1.height));

//                //imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

//                //result.Add(imageInfo);
//            }

//            return result;
//        }

//        public void Save(Stream output, IList<ImageInfo> imageInfo)
//        {

//        }
//    }
//}
