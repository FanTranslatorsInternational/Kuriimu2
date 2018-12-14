using System;
using System.IO;
using System.Text;
using Komponent.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KomponentUnitTests
{
    [TestClass]
    public class BinaryReaderXTests
    {
        [TestMethod]
        public void BaseReads()
        {
            var input = new byte[] {
                0x00, 0x11, 0x21, 0x22, 0x32, 0x33, 0x43, 0x44,
                0x44, 0x44, 0x54, 0x55, 0x55, 0x55, 0x01, 0x65,
                0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x76,
                0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0xf8,
                0x53, 0xe3, 0x3d, 0xd1, 0x22, 0xdb, 0xf9, 0x7e,
                0x6a, 0xbc, 0x3f, 0x3f, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                Assert.AreEqual(0x00, br.ReadSByte());
                Assert.AreEqual(0x11, br.ReadByte());
                Assert.AreEqual(0x2221, br.ReadInt16());
                Assert.AreEqual(0x3332, br.ReadUInt16());
                Assert.AreEqual(0x44444443, br.ReadInt32());
                Assert.AreEqual(0x55555554u, br.ReadUInt32());
                Assert.AreEqual(true, br.ReadBoolean());
                Assert.AreEqual(0x6666666666666665, br.ReadInt64());
                Assert.AreEqual(0x7777777777777776u, br.ReadUInt64());

                Assert.AreEqual(0.111f, br.ReadSingle());
                Assert.AreEqual(0.111, br.ReadDouble());
                Assert.AreEqual((decimal)63.0, br.ReadDecimal());
            }
        }

        [TestMethod]
        public void CharStringReads()
        {
            var input = new byte[] {
                0x33, 0x00, 0x34, 0x00, 0x34, 0x00, 0x33, 0x34,
                0x00, 0x33, 0x34, 0x00, 0x34, 0x00, 0x34, 0x00,
                0x00, 0x00, 0x34, 0x00, 0x34, 0x34, 0x34, 0x34,
                0x35, 0x35, 0x35, 0x35, 0x35, 0x36, 0x36
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                Assert.AreEqual('3', br.ReadChar());
                Assert.AreEqual("44", br.ReadChars(2).Aggregate("", (a, b) => a + b));

                Assert.AreEqual("34", br.ReadCStringASCII());
                Assert.AreEqual("34", br.ReadCStringSJIS());
                Assert.AreEqual("44", br.ReadCStringUTF16());

                Assert.AreEqual("4", br.ReadString());
                Assert.AreEqual("44", br.ReadString(2));
                Assert.AreEqual("44", br.ReadString(2, Encoding.UTF8));

                Assert.AreEqual("5555", br.PeekString());
                Assert.AreEqual("55555", br.PeekString(5));
                Assert.AreEqual("555", br.PeekString(3, Encoding.UTF8));
                Assert.AreEqual("5566", br.PeekString(3L));
                Assert.AreEqual("556", br.PeekString(3L, 3));
                Assert.AreEqual("556", br.PeekString(3L, 3, Encoding.UTF8));
            }
        }
    }
}
