using Kryptography.AES;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KryptographyUnitTests
{
    [TestClass]
    public class EcbTests
    {
        [TestMethod]
        public void InitializeStream()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            Assert.ThrowsException<InvalidOperationException>(() => new EcbStream(new MemoryStream(new byte[] { 0, 0 }), key));
        }

        [TestMethod]
        public void ReadTest1()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var ms = new MemoryStream(content);
            var ecb = new EcbStream(ms, key);
            var result = new byte[16];
            var expected = new byte[16];

            ecb.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(16, ecb.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void ReadTest2()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e,
            0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var ms = new MemoryStream(content);
            var ecb = new EcbStream(ms, key);
            var result = new byte[16];
            var expected = new byte[16];

            ecb.Position = 16;
            ecb.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(32, ecb.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest1()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var expected = new byte[] { 0x40, 0x15, 0xaf, 0xf5, 0x6f, 0xb5, 0xb9, 0x20, 0xc9, 0x55, 0x44, 0x90, 0x35, 0xda, 0xa7, 0x41 };
            var ms = new MemoryStream(content);
            var ecb = new EcbStream(ms, key);

            ecb.Position = 3;
            ecb.Write(new byte[] { 0x11, 0x11 }, 0, 2);
            ecb.Flush();

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(5, ecb.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest2()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var expected = new byte[] { 0x04, 0x10, 0xbe, 0xfc, 0xcd, 0xe6, 0x94, 0x4b, 0x69, 0xdd, 0x00, 0x7d, 0xeb, 0xe3, 0x9a, 0x9d,
            0x3a, 0xb6, 0x07, 0x36, 0xf9, 0xcd, 0x20, 0xa2, 0xcf, 0xe1, 0xf2, 0x48, 0x34, 0xb6, 0x25, 0x3d};
            var ms = new MemoryStream();
            ms.Write(content, 0, content.Length);
            ms.Position = 0;
            var ecb = new EcbStream(ms, key);

            ecb.Position = 15;
            ecb.Write(new byte[] { 0x11, 0x11 }, 0, 2);
            var actualLength = ecb.Length;
            ecb.Flush();
            var flushedLength = ecb.Length;

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(32, ecb.Length);
            Assert.AreEqual(17, actualLength);
            Assert.AreEqual(32, flushedLength);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest3()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e,
                0x4c, 0x02, 0x1d, 0x6d, 0xf5, 0x51, 0xd7, 0xf8, 0x74, 0xa4, 0xe0, 0x30, 0x8f, 0x4c, 0xb6, 0x13 };
            var ms = new MemoryStream();
            ms.Write(content, 0, content.Length);
            ms.Position = 0;
            var ecb = new EcbStream(ms, key);

            ecb.Position = 17;
            ecb.Write(new byte[] { 0x11, 0x11 }, 0, 2);
            var actualLength = ecb.Length;
            ecb.Flush();
            var flushedLength = ecb.Length;

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(32, ecb.Length);
            Assert.AreEqual(19, actualLength);
            Assert.AreEqual(32, flushedLength);
            Assert.AreEqual(0, ms.Position);
        }
    }
}
