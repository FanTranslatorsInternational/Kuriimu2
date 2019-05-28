using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression;
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

            var br = new BitReader(new MemoryStream(input), BitOrder.MSBFirst);
            var bitValue = br.ReadBit();
            var byteValue = br.ReadByte();
            var intValue = br.ReadInt32();

            Assert.AreEqual(48, br.Length);
            Assert.AreEqual(41, br.Position);
            Assert.AreEqual(0, bitValue);
            Assert.AreEqual(0x19, byteValue);
            Assert.AreEqual(0xFF999999, (uint)intValue);
        }
    }
}
