//using System.IO;
//using Kompression;
//using Kompression.IO;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace KompressionUnitTests
//{
//    [TestClass]
//    public class BitReaderTests
//    {
//        [TestMethod]
//        public void ReadMsbTests()
//        {
//            var input = new byte[]
//                {0b00001100, 0b11111111, 0b11001100, 0b10001100, 0b11001100, 0b10000010, 0b11110000, 0b00000001};

//            var br = new BitReader(new MemoryStream(input), BitOrder.MsbFirst, 1, ByteOrder.LittleEndian);
//            var bitValue = br.ReadBit();
//            var byteValue = br.ReadByte();
//            var shortValue = br.ReadInt16();
//            var intValue = br.ReadInt32();
//            var bitValue2 = br.ReadBits<int>(6);
//            var bitValue3 = br.SeekBits<int>(1);

//            Assert.AreEqual(64, br.Length);
//            Assert.AreEqual(63, br.Position);
//            Assert.AreEqual(0, bitValue);
//            Assert.AreEqual(0b00011001, byteValue);
//            Assert.AreEqual(0b1111111110011001, shortValue);
//            Assert.AreEqual(0b00011001100110010000010111100000, intValue);
//            Assert.AreEqual(0b000000, bitValue2);
//            Assert.AreEqual(1,bitValue3);
//        }

//        [TestMethod]
//        public void ReadLsbTests()
//        {
//            var input = new byte[]
//                {0b00001100, 0b11111111, 0b11001100, 0b10001100, 0b11001100, 0b10000010, 0b11110000, 0b10000000};

//            var br = new BitReader(new MemoryStream(input), BitOrder.LsbFirst, 1, ByteOrder.LittleEndian);
//            var bitValue = br.ReadBit();
//            var byteValue = br.ReadByte();
//            var shortValue = br.ReadInt16();
//            var intValue = br.ReadInt32();
//            var bitValue2 = br.ReadBits<int>(6);
//            var bitValue3 = br.SeekBits<int>(1);

//            Assert.AreEqual(64, br.Length);
//            Assert.AreEqual(63, br.Position);
//            Assert.AreEqual(0, bitValue);
//            Assert.AreEqual(0b10000110, byteValue);
//            Assert.AreEqual(0b0110011001111111, shortValue);
//            Assert.AreEqual(0b01111000010000010110011001000110, intValue);
//            Assert.AreEqual(0b000000, bitValue2);
//            Assert.AreEqual(1, bitValue3);
//        }

//        [TestMethod]
//        public void Read4ByteLETests()
//        {
//            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xC0, 0x0C, 0x00, 0x00 };

//            var br = new BitReader(new MemoryStream(input), BitOrder.MsbFirst, 4, ByteOrder.LittleEndian);
//            var bitValue = br.ReadBit();
//            var byteValue = br.ReadByte();
//            var intValue = br.ReadInt32();

//            Assert.AreEqual(64, br.Length);
//            Assert.AreEqual(41, br.Position);
//            Assert.AreEqual(1, bitValue);
//            Assert.AreEqual(0x99, byteValue);
//            Assert.AreEqual(0x99FE1800, (uint)intValue);
//        }

//        [TestMethod]
//        public void Read4ByteBETests()
//        {
//            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xCC, 0x80, 0x00, 0x00 };

//            var br = new BitReader(new MemoryStream(input), BitOrder.MsbFirst, 4, ByteOrder.BigEndian);
//            var bitValue = br.ReadBit();
//            var byteValue = br.ReadByte();
//            var intValue = br.ReadInt32();

//            Assert.AreEqual(64, br.Length);
//            Assert.AreEqual(41, br.Position);
//            Assert.AreEqual(0, bitValue);
//            Assert.AreEqual(0x19, byteValue);
//            Assert.AreEqual(0xFF999999, (uint)intValue);
//        }
//    }
//}
