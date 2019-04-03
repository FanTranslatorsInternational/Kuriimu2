using Kryptography.AES;
using Kryptography.AES.XTS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KryptographyUnitTests
{
    [TestClass]
    public class XtsTests
    {
        public void Encrypt(byte[] content, byte[] sectorId)
        {
            var aes = AesXts.Create(false, 0x20);
            var enc = aes.CreateEncryptor(new byte[0x20], sectorId);
            enc.TransformBlock(content, 0, content.Length, content, 0);
        }

        [TestMethod]
        public void XtsContextTest()
        {
            var aes = AesXts.Create(false, 0x20);
            var enc = aes.CreateEncryptor(new byte[0x20], new byte[0x10]);
            var dec = aes.CreateDecryptor(new byte[0x20], new byte[0x10]);

            var content = new byte[0x10];
            enc.TransformBlock(content, 0, content.Length, content, 0);
            dec.TransformBlock(content, 0, content.Length, content, 0);

            Assert.IsTrue(new byte[0x10].SequenceEqual(content));
        }

        [TestMethod]
        public void InitializeStream()
        {
            Assert.ThrowsException<InvalidOperationException>(() => new XtsStream(new MemoryStream(new byte[2]), new byte[32], new byte[16], false, 0x30));
            Assert.ThrowsException<InvalidOperationException>(() => new XtsStream(new MemoryStream(new byte[16]), new byte[32], new byte[16], false, 0x27));
        }

        [TestMethod]
        public void ReadTest1()
        {
            var key = new byte[32];
            var final = new byte[] { 0x3a, 0x18, 0xda, 0xa2, 0xca, 0x3b, 0x3a, 0x64, 0x58, 0xe5, 0xaf, 0xac, 0xbc, 0x48, 0x95, 0x8e };
            var ms = new MemoryStream(final);
            var xts = new XtsStream(ms, key, new byte[16], false, 0x30);
            var result = new byte[16];
            var expected = new byte[16];
            expected[0xf] = 0x01;

            xts.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(16, xts.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void ReadTest2()
        {
            var toEncrypt = new byte[0x10];
            var sectorId = new byte[0x10];
            sectorId[0xf] = 0x01;
            Encrypt(toEncrypt, sectorId);

            var content = new byte[0x30];
            Array.Copy(toEncrypt, 0, content, 0x20, 0x10);
            var ms = new MemoryStream(content);
            var xts = new XtsStream(ms, new byte[32], new byte[16], false, 0x20);
            var result = new byte[16];
            var expected = new byte[16];

            xts.Position = 32;
            xts.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(48, xts.Position);
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
            Assert.AreEqual(16, ms.Position);
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
            Assert.AreEqual(16, ms.Position);
        }
    }
}
