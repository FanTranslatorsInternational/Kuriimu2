using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KomponentUnitTests.ByteReader
{
    [TestClass]
    public class AlignmentTests
    {
        [TestMethod]
        public void AlignmentSeek()
        {
            var input = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                Assert.AreEqual(0x01, br.ReadByte());
                br.SeekAlignment(8);
                Assert.AreEqual(0x8, br.BaseStream.Position);
                Assert.AreEqual(0x2, br.ReadByte());

                br.BaseStream.Position = 0;
                Assert.AreEqual(0x01, br.ReadByte());
                br.SeekAlignment();
                Assert.AreEqual(0x10, br.BaseStream.Position);
                Assert.AreEqual(0x3, br.ReadByte());
            }
        }

        public void AlignmentAttributeRead()
        {
            var input = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                var rs = br.ReadType<TestClass>();

                Assert.AreEqual(0x10, br.BaseStream.Position);
            }
        }

        [Alignment(16)]
        private class TestClass
        {
            public int var0;
            public int var1;
            public byte var2;
        }
    }
}
