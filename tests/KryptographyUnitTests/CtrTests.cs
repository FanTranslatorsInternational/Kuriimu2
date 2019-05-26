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
    public class CtrTests
    {
        [TestMethod]
        public void InitializeStream()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            Assert.ThrowsException<InvalidOperationException>(() => new CtrStream(new MemoryStream(new byte[] { 0, 0 }), key, ctr, false));
        }

        [TestMethod]
        public void ReadTest1()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x3f };
            var ms = new MemoryStream(content);
            var ctrS = new CtrStream(ms, key, ctr, false);
            var result = new byte[16];
            var expected = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x11 };

            ctrS.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(16, ctrS.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void ReadTest2()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x3f,
                0x58, 0xe2, 0xfc, 0xce, 0xfa, 0x7e, 0x30, 0x61, 0x36, 0x7f, 0x1d, 0x57, 0xa4, 0xe7, 0x45, 0x4b };
            var ms = new MemoryStream(content);
            var ctrS = new CtrStream(ms, key, ctr, false);
            var result = new byte[16];
            var expected = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x11 };

            ctrS.Position = 16;
            ctrS.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(32, ctrS.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void ReadTest3()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e,
                0x47, 0x71, 0x18, 0x16, 0xe9, 0x1d, 0x6f, 0xf0, 0x59, 0xbb, 0xbf, 0x2b, 0xf5, 0x8e, 0x0f, 0xc2 };
            var ms = new MemoryStream(content);
            var ctrS = new CtrStream(ms, key, ctr, true);
            var result = new byte[16];
            var expected = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x11 };

            ctrS.Position = 16;
            ctrS.Read(result, 0, 16);

            Assert.IsTrue(result.SequenceEqual(expected));
            Assert.AreEqual(32, ctrS.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest1()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xc5, 0xfe, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var ms = new MemoryStream(content);
            var ctrS = new CtrStream(ms, key, ctr, false);

            ctrS.Position = 3;
            ctrS.Write(new byte[] { 0x11, 0x11 }, 0, 2);
            ctrS.Flush();

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(5, ctrS.Position);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest2()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x3f,
                0x49, 0xe2, 0xfc, 0xce, 0xfa, 0x7e, 0x30, 0x61, 0x36, 0x7f, 0x1d, 0x57, 0xa4, 0xe7, 0x45, 0x5a };
            var ms = new MemoryStream();
            ms.Write(content, 0, content.Length);
            ms.Position = 0;
            var ctrS = new CtrStream(ms, key, ctr, false);

            ctrS.Position = 15;
            ctrS.Write(new byte[] { 0x11, 0x11 }, 0, 2);
            var actualLength = ctrS.Length;
            ctrS.Flush();
            var flushedLength = ctrS.Length;

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(32, ctrS.Length);
            Assert.AreEqual(17, actualLength);
            Assert.AreEqual(32, flushedLength);
            Assert.AreEqual(0, ms.Position);
        }

        [TestMethod]
        public void WriteTest3()
        {
            var key = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var ctr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var content = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e };
            var expected = new byte[] { 0x66, 0xe9, 0x4b, 0xd4, 0xef, 0x8a, 0x2c, 0x3b, 0x88, 0x4c, 0xfa, 0x59, 0xca, 0x34, 0x2b, 0x2e,
                0x58, 0xf3, 0xed, 0xce, 0xfa, 0x7e, 0x30, 0x61, 0x36, 0x7f, 0x1d, 0x57, 0xa4, 0xe7, 0x45, 0x5a };
            var ms = new MemoryStream();
            ms.Write(content, 0, content.Length);
            ms.Position = 0;
            var ctrS = new CtrStream(ms, key, ctr, false);

            ctrS.Position = 17;
            ctrS.Write(new byte[] { 0x11, 0x11 }, 0, 2);
            var actualLength = ctrS.Length;
            ctrS.Flush();
            var flushedLength = ctrS.Length;

            Assert.IsTrue(ms.ToArray().SequenceEqual(expected));
            Assert.AreEqual(32, ctrS.Length);
            Assert.AreEqual(19, actualLength);
            Assert.AreEqual(32, flushedLength);
            Assert.AreEqual(0, ms.Position);
        }
    }
}
