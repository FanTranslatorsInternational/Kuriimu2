//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Kompression;
//using Kompression.IO;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace KompressionUnitTests
//{
//    [TestClass]
//    public class BitWriterTests
//    {
//        [TestMethod]
//        public void WriteTests()
//        {
//            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xCC, 0x80 };
//            var ms = new MemoryStream();

//            var bw = new BitWriter(ms, BitOrder.MsbFirst, 1, ByteOrder.BigEndian);
//            bw.WriteBit(0);
//            bw.WriteByte(0x19);
//            unchecked
//            {
//                bw.WriteInt32((int)0xFF999999);
//            }
//            bw.Flush();

//            Assert.AreEqual(48, bw.Length);
//            Assert.AreEqual(48, bw.Position);
//            Assert.IsTrue(ms.ToArray().SequenceEqual(input));
//        }

//        [TestMethod]
//        public void Write4ByteLETests()
//        {
//            var input = new byte[] { 0xCC, 0xCC, 0xFF, 0x0C, 0x00, 0x00, 0x80, 0xCC };
//            var ms = new MemoryStream();

//            var bw = new BitWriter(ms, BitOrder.MsbFirst, 4, ByteOrder.LittleEndian);
//            bw.WriteBit(0);
//            bw.WriteByte(0x19);
//            unchecked
//            {
//                bw.WriteInt32((int)0xFF999999);
//            }
//            bw.Flush();

//            Assert.AreEqual(64, bw.Length);
//            Assert.AreEqual(64, bw.Position);
//            Assert.IsTrue(ms.ToArray().SequenceEqual(input));
//        }

//        [TestMethod]
//        public void Write4ByteBETests()
//        {
//            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xCC, 0x80, 0x00, 0x00 };
//            var ms = new MemoryStream();

//            var bw = new BitWriter(ms, BitOrder.MsbFirst, 4, ByteOrder.BigEndian);
//            bw.WriteBit(0);
//            bw.WriteByte(0x19);
//            unchecked
//            {
//                bw.WriteInt32((int)0xFF999999);
//            }
//            bw.Flush();

//            Assert.AreEqual(64, bw.Length);
//            Assert.AreEqual(64, bw.Position);
//            Assert.IsTrue(ms.ToArray().SequenceEqual(input));
//        }
//    }
//}
