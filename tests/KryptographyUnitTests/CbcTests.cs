using Kryptography.AES;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace KryptographyUnitTests
{
    [TestClass]
    public class CbcTests
    {
        [TestMethod]
        public void InitializeStream()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            Assert.ThrowsException<InvalidOperationException>(() => new CbcStream(new MemoryStream(new byte[] { 0, 0 }), key, iv));
        }

        [TestMethod]
        public void ReadTest1()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var ms = new MemoryStream(content);
            var cbc = new CbcStream(ms, key, iv);
            var result = new byte[16];
            var expected = new byte[16];

            cbc.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(16, cbc.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void ReadTest2()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e,
            0xf7, 0x95, 0xbd, 0x4a, 0x52, 0xe2, 0x9e, 0xd7, 0x13, 0xd3, 0x13, 0xfa, 0x20, 0xe9, 0x8d, 0xbc };
            var ms = new MemoryStream(content);
            var cbc = new CbcStream(ms, key, iv);
            var result = new byte[16];
            var expected = new byte[16];

            cbc.Position = 16;
            cbc.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(32, cbc.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest1()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var ms = new MemoryStream();
            var cbc = new CbcStream(ms, key, iv);

            cbc.Write(new byte[16], 0, 16);
            cbc.Flush();

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(16, cbc.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest2()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e,
            0xf7, 0x95, 0xbd, 0x4a, 0x52, 0xe2, 0x9e, 0xd7, 0x13, 0xd3, 0x13, 0xfa, 0x20, 0xe9, 0x8d, 0xbc };
            var ms = new MemoryStream();
            var cbc = new CbcStream(ms, key, iv);

            cbc.Write(new byte[12], 0, 12);
            cbc.Write(new byte[4], 0, 4);
            cbc.Write(new byte[16], 0, 16);
            cbc.Flush();

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(32, cbc.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest3()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var ms = new MemoryStream();
            var cbc = new CbcStream(ms, key, iv);

            cbc.Write(new byte[16], 0, 16);
            cbc.Position = 12;

            Assert.ThrowsException<InvalidOperationException>(() => cbc.Write(new byte[4], 0, 4));
            Assert.AreEqual(12, cbc.Position);
        }
    }
}
