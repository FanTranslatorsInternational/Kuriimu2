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
            var aes = AesXts.Create(false, 0x20, true);
            var enc = aes.CreateEncryptor(new byte[0x20], sectorId);
            enc.TransformBlock(content, 0, content.Length, content, 0);
        }

        public void Decrypt(byte[] content, byte[] sectorId)
        {
            var aes = AesXts.Create(false, 0x20, true);
            var enc = aes.CreateDecryptor(new byte[0x20], sectorId);
            enc.TransformBlock(content, 0, content.Length, content, 0);
        }

        [TestMethod]
        public void XtsContextTest()
        {
            var aes = AesXts.Create(false, 0x20, true);
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
            Assert.ThrowsException<InvalidOperationException>(() => new XtsStream(new MemoryStream(new byte[2]), new byte[32], new byte[16], true, false, 0x30));
            Assert.ThrowsException<InvalidOperationException>(() => new XtsStream(new MemoryStream(new byte[16]), new byte[32], new byte[16], true, false, 0x27));
        }

        [TestMethod]
        public void ReadTest1()
        {
            var key = new byte[32];
            var final = new byte[] { 0x3a, 0x18, 0xda, 0xa2, 0xca, 0x3b, 0x3a, 0x64, 0x58, 0xe5, 0xaf, 0xac, 0xbc, 0x48, 0x95, 0x8e };
            var ms = new MemoryStream(final);
            var xts = new XtsStream(ms, key, new byte[16], true, false, 0x30);
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
            var xts = new XtsStream(ms, new byte[32], new byte[16], true, false, 0x20);
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
            var expected = new byte[0x30];
            expected[0x21] = 0x02;
            Encrypt(expected, new byte[0x10]);

            var content = new byte[0x30];
            content[0x21] = 0x02;
            Encrypt(content, new byte[0x10]);
            var ms = new MemoryStream(content);
            var xts = new XtsStream(ms, new byte[0x20], new byte[0x10], true, false, 0x20);

            xts.Position = 0x21;
            xts.Write(new byte[] { 0x02 }, 0, 1);
            xts.Flush();

            Assert.AreEqual(0x30, xts.Length);
            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(0x22, xts.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest2()
        {
            var expected = new byte[0x40];
            expected[0x2f] = 0x02;
            expected[0x30] = 0x02;
            Encrypt(expected, new byte[0x10]);

            var content = new byte[0x30];
            Encrypt(content, new byte[0x10]);
            var ms = new MemoryStream();
            ms.Write(content, 0, 0x30);
            ms.Position = 0;
            var xts = new XtsStream(ms, new byte[0x20], new byte[0x10], true, false, 0x20);

            xts.Position = 0x2f;
            xts.Write(new byte[] { 0x02, 0x02 }, 0, 2);
            xts.Flush();

            Assert.AreEqual(0x40, xts.Length);
            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(0x40, xts.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest3()
        {
            var expected = new byte[0x40];
            expected[0x35] = 0x02;
            expected[0x36] = 0x02;
            Encrypt(expected, new byte[0x10]);

            var content = new byte[0x30];
            Encrypt(content, new byte[0x10]);
            var ms = new MemoryStream();
            ms.Write(content, 0, 0x30);
            ms.Position = 0;
            var xts = new XtsStream(ms, new byte[0x20], new byte[0x10], true, false, 0x20);

            xts.Position = 0x35;
            xts.Write(new byte[] { 0x02, 0x02 }, 0, 2);
            xts.Flush();

            Assert.AreEqual(0x40, xts.Length);
            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(0x40, xts.Position);
            Assert.AreEqual(0, ms.Position);
        }
    }
}
