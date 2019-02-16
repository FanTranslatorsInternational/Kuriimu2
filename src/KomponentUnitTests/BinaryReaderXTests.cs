using System;
using System.IO;
using System.Text;
using Komponent.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
                0x33, 0x34, 0x34, 0x33, 0x34, 0x00, 0x33, 0x34,
                0x00, 0x34, 0x00, 0x34, 0x00, 0x00, 0x00, 0x01,
                0x34, 0x34, 0x34, 0x34, 0x00, 0x34, 0x00,
                0x35, 0x35, 0x35, 0x35, 0x35, 0x36, 0x36
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                Assert.AreEqual('3', br.ReadChar());
                Assert.AreEqual("44", string.Join("", br.ReadChars(2).ToArray()));

                Assert.AreEqual("34", br.ReadCStringASCII());
                Assert.AreEqual("34", br.ReadCStringSJIS());
                Assert.AreEqual("44", br.ReadCStringUTF16());

                Assert.AreEqual("4", br.ReadString());
                Assert.AreEqual("44", br.ReadString(2));
                Assert.AreEqual("44", br.ReadString(4, Encoding.Unicode));

                Assert.AreEqual("5555", br.PeekString());
                Assert.AreEqual("55555", br.PeekString(5));
                Assert.AreEqual("555", br.PeekString(3, Encoding.ASCII));
                Assert.AreEqual("5566", br.PeekString(3L));
                Assert.AreEqual("556", br.PeekString(3L, 3));
                Assert.AreEqual("556", br.PeekString(3L, 3, Encoding.ASCII));

                Assert.AreEqual("55555", br.ReadASCIIStringUntil(0x36));
            }
        }

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

        [TestMethod]
        public void BitReading()
        {
            var input = new byte[] {
                0x00, 0x80, 0x1F, 0xF8, 0xFF, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms, true, ByteOrder.LittleEndian, BitOrder.MSBFirst, 2))
            {
                Assert.AreEqual(true, br.ReadBit());
                br.ResetBitBuffer();

                Assert.AreEqual(0x1F, br.ReadBits<int>(5));
                Assert.AreEqual(0x00, br.ReadBits<int>(6));
                Assert.AreEqual(0x1F, br.ReadBits<int>(5));
            }

            using (var br = new BinaryReaderX(ms, ByteOrder.LittleEndian, BitOrder.LowestAddressFirst, 2))
            {
                br.BaseStream.Position = 0;

                Assert.AreEqual(false, br.ReadBit());
                br.ResetBitBuffer();

                br.ByteOrder = ByteOrder.BigEndian;
                br.BaseStream.Position = 4;
                Assert.AreEqual(0xFF, br.ReadBits<int>(8));
            }
        }

        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        [BitFieldInfo(BlockSize = 1)]
        private class TestClass
        {
            public bool exp0;
            public int exp1;
            public TestClass2 exp2;
            [FixedLength(3)]
            public byte[] exp3;
            [VariableLength("exp1")]
            public List<byte> exp4;
            [VariableLength("exp1", Offset = 2, StringEncoding = StringEncoding.UTF8)]
            public string exp5;
            public TestClass3 exp6;
            [BitField(6)]
            public byte exp7;

            [BitFieldInfo(BitOrder = BitOrder.LSBFirst, BlockSize = 2)]
            public class TestClass2
            {
                [BitField(5)]
                public short val1;
                [BitField(6)]
                public short val2;
                [BitField(5)]
                public short val3;
            }

            [Endianness(ByteOrder = ByteOrder.LittleEndian)]
            public class TestClass3
            {
                public int val1;
            }
        }

        [TestMethod]
        public void GenericReading()
        {
            var input = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x04, 0xF8, 0x0F, 0x11,
                0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0x34, 0x34,
                0x34, 0x35, 0x36, 0x34, 0x05, 0x00, 0x00, 0x00,
                0xFF,
                0x01, 0x00, 0x00, 0x00, 0x04, 0xF8, 0x1F, 0x11,
                0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0x34, 0x34,
                0x34, 0x35, 0x36, 0x34, 0x05, 0x00, 0x00, 0x00,
                0xFF
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                var examp = br.ReadStruct<TestClass>();

                Assert.AreEqual(true, examp.exp0);
                Assert.AreEqual(0x04, examp.exp1);
                Assert.AreEqual(0x0F, examp.exp2.val1);
                Assert.AreEqual(0x00, examp.exp2.val2);
                Assert.AreEqual(0x1F, examp.exp2.val3);
                Assert.AreEqual(0x11, examp.exp3[0]);
                Assert.AreEqual(0x11, examp.exp3[1]);
                Assert.AreEqual(0x11, examp.exp3[2]);
                Assert.AreEqual(0x22, examp.exp4[0]);
                Assert.AreEqual(0x22, examp.exp4[1]);
                Assert.AreEqual(0x22, examp.exp4[2]);
                Assert.AreEqual(0x22, examp.exp4[3]);
                Assert.AreEqual("444564", examp.exp5);
                Assert.AreEqual(0x05, examp.exp6.val1);
                Assert.AreEqual(0x3F, examp.exp7);

                br.BaseStream.Position = 0;
                var exampList = br.ReadMultiple<TestClass>(2);
                var index = 0;
                foreach (var exampEntry in exampList)
                {
                    Assert.AreEqual(true, exampEntry.exp0);
                    Assert.AreEqual(0x04, exampEntry.exp1);
                    Assert.AreEqual((index == 0) ? 0x0F : 0x1F, exampEntry.exp2.val1);
                    Assert.AreEqual(0x00, exampEntry.exp2.val2);
                    Assert.AreEqual(0x1F, exampEntry.exp2.val3);
                    Assert.AreEqual(0x11, exampEntry.exp3[0]);
                    Assert.AreEqual(0x11, exampEntry.exp3[1]);
                    Assert.AreEqual(0x11, exampEntry.exp3[2]);
                    Assert.AreEqual(0x22, exampEntry.exp4[0]);
                    Assert.AreEqual(0x22, exampEntry.exp4[1]);
                    Assert.AreEqual(0x22, exampEntry.exp4[2]);
                    Assert.AreEqual(0x22, exampEntry.exp4[3]);
                    Assert.AreEqual("444564", exampEntry.exp5);
                    Assert.AreEqual(0x05, exampEntry.exp6.val1);
                    Assert.AreEqual(0x3F, examp.exp7);

                    index++;
                }
            }
        }

        [TestMethod]
        public void NibbleReading()
        {
            var input = new byte[] {
                0x48
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                Assert.AreEqual(8, br.ReadNibble());
                Assert.AreEqual(4, br.ReadNibble());
            }
        }

        [TestMethod]
        public void SwitchNibbleBitReading()
        {
            var input = new byte[] {
                0x04, 0x1F, 0xF8, 0x80
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms, ByteOrder.LittleEndian, BitOrder.MSBFirst, 2))
            {
                Assert.AreEqual(4, br.ReadNibble());
                Assert.AreEqual(0x1F, br.ReadBits<int>(5));
                Assert.AreEqual(0, br.ReadNibble());
                Assert.AreEqual(8, br.ReadNibble());
            }
        }
    }
}
