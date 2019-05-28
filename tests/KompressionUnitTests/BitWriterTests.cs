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
    public class BitWriterTests
    {
        [TestMethod]
        public void WriteTests()
        {
            var input = new byte[] { 0x0C, 0xFF, 0xCC, 0xCC, 0xCC, 0x80 };
            var ms = new MemoryStream();

            var bw = new BitWriter(ms, BitOrder.MSBFirst);
            bw.WriteBit(0);
            bw.WriteByte(0x19);
            unchecked
            {
                bw.WriteInt32((int)0xFF999999);
            }
            bw.Flush();

            Assert.AreEqual(48, bw.Length);
            Assert.AreEqual(48, bw.Position);
            Assert.IsTrue(ms.ToArray().SequenceEqual(input));
        }
    }
}
