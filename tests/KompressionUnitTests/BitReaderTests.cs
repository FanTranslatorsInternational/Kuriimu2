using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression;
using Kompression.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KompressionUnitTests
{
    [TestClass]
    public class BitReaderTests
    {
        [TestMethod]
        public void ReadTests()
        {
            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xCC, 0x80 };

            var br = new BitReader(new MemoryStream(input), BitOrder.MSBFirst, 1, ByteOrder.LittleEndian);
            var bitValue = br.ReadBit();
            var byteValue = br.ReadByte();
            var intValue = br.ReadInt32();

            Assert.AreEqual(48, br.Length);
            Assert.AreEqual(41, br.Position);
            Assert.AreEqual(0, bitValue);
            Assert.AreEqual(0x19, byteValue);
            Assert.AreEqual(0xFF999999, (uint)intValue);
        }

        [TestMethod]
        public void Read4ByteLETests()
        {
            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xC0, 0x0C, 0x00, 0x00 };

            var br = new BitReader(new MemoryStream(input), BitOrder.MSBFirst, 4, ByteOrder.LittleEndian);
            var bitValue = br.ReadBit();
            var byteValue = br.ReadByte();
            var intValue = br.ReadInt32();

            Assert.AreEqual(64, br.Length);
            Assert.AreEqual(41, br.Position);
            Assert.AreEqual(1, bitValue);
            Assert.AreEqual(0x99, byteValue);
            Assert.AreEqual(0x99FE1800, (uint)intValue);
        }

        [TestMethod]
        public void Read4ByteBETests()
        {
            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xCC, 0x80, 0x00, 0x00 };

            var br = new BitReader(new MemoryStream(input), BitOrder.MSBFirst, 4, ByteOrder.BigEndian);
            var bitValue = br.ReadBit();
            var byteValue = br.ReadByte();
            var intValue = br.ReadInt32();

            Assert.AreEqual(64, br.Length);
            Assert.AreEqual(41, br.Position);
            Assert.AreEqual(0, bitValue);
            Assert.AreEqual(0x19, byteValue);
            Assert.AreEqual(0xFF999999, (uint)intValue);
        }

        [TestMethod]
        public void ReadBits()
        {
            var input = new byte[] { 0xC0 };

            var br = new BitReader(new MemoryStream(input), BitOrder.MSBFirst, 1, ByteOrder.BigEndian);
            var bitValue = br.ReadBits<byte>(4);

            Assert.AreEqual(0xC, bitValue);
        }
    }
}
