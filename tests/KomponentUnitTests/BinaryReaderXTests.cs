using System;
using System.IO;
using System.Text;
using Komponent.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Komponent.IO.Attributes;
using Kontract.Models.IO;

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
        public void BitReading()
        {
            var input = new byte[] {
                0x00, 0x80, 0x1F, 0xF8, 0xFF, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms, true, ByteOrder.LittleEndian, BitOrder.MostSignificantBitFirst, 2))
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

        private class TestClass0
        {

        }

        [TestMethod]
        public void BitReading2()
        {
            var input = new byte[] {
                0x80, 0x30, 0x30, 0x02, 0x40, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms, true))
            {
                Assert.AreEqual(0x08c, br.ReadBits<int>(14));
                Assert.AreEqual(0x308, br.ReadBits<int>(14));
                Assert.AreEqual(0x00, br.ReadBits<int>(4));
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

            [BitFieldInfo(BitOrder = BitOrder.LeastSignificantBitFirst, BlockSize = 2)]
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
                var examp = br.ReadType<TestClass>();

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

        private class TestClass2
        {
            public int var0;
            public TestClass2_2 var1;
            [VariableLength("var1.var2")]
            public byte[] var3;
            public TestClass2_3 var4;

            public class TestClass2_2
            {
                public byte var2;
            }

            public class TestClass2_3
            {
                public byte var0;
                [VariableLength("var0")]    // field names go from root of readstruct
                public byte[] var5;
            }
        }

        [TestMethod]
        public void VariableLengthNestedFields()
        {
            var input = new byte[] {
                0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                0x04, 0x03, 0x00, 0x00, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                var rs = br.ReadType<TestClass2>();

                Assert.AreEqual(2, rs.var0);
                Assert.AreEqual(3, rs.var1.var2);
                Assert.AreEqual(3, rs.var3.Length);
                Assert.AreEqual(4, rs.var4.var0);
                Assert.AreEqual(2, rs.var4.var5.Length);
                Assert.AreEqual(3, rs.var4.var5[0]);
                Assert.AreEqual(0, rs.var4.var5[1]);
            }
        }

        private class TestClass3
        {
            public TestClass31 var0;
            [VariableLength("var0.var1")]
            public byte[] var1;

            public class TestClass31
            {
                public int var0;
                public int var1;
            }
        }

        [TestMethod]
        public void ClassFirstNestedIssue()
        {
            var input = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x02, 0x02
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                var rs = br.ReadType<TestClass3>();

                Assert.AreEqual(1, rs.var0.var0);
                Assert.AreEqual(2, rs.var0.var1);
                Assert.AreNotEqual(null, rs.var1);
                Assert.AreEqual(2, rs.var1.Length);
                Assert.AreEqual(2, rs.var1[0]);
                Assert.AreEqual(2, rs.var1[1]);
            }
        }

        private class TestClass4
        {
            public int flag;

            [TypeChoice("flag", TypeChoiceComparer.Equal, 0x00000001, typeof(int))]
            [TypeChoice("flag", TypeChoiceComparer.GEqual, 0x00000002, typeof(long))]
            public object value;

            [TypeChoice("flag", TypeChoiceComparer.SEqual, 0x00000001, typeof(float))]
            [TypeChoice("flag", TypeChoiceComparer.Greater, 0x00000001, typeof(double))]
            public object value2;

            [TypeChoice("flag", TypeChoiceComparer.Smaller, 0x00000002, typeof(decimal))]
            [TypeChoice("flag", TypeChoiceComparer.Equal, 0x00000002, typeof(bool))]
            public object value3;
        }

        [TestMethod]
        public void TypeChoiceRead()
        {
            var input = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xc0, 0x3f, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                var tc4 = br.ReadType<TestClass4>();

                Assert.AreEqual(typeof(int), tc4.value.GetType());
                Assert.AreEqual(2, tc4.value);
                Assert.AreEqual(typeof(float), tc4.value2.GetType());
                Assert.AreEqual(1.5f, tc4.value2);
                Assert.AreEqual(typeof(decimal), tc4.value3.GetType());
                Assert.AreEqual((decimal)0.0, tc4.value3);

                ms.Position = 0;
                ms.Write(new byte[] { 0x02 }, 0, 1);
                ms.Position = 0;

                tc4 = br.ReadType<TestClass4>();

                Assert.AreEqual(typeof(long), tc4.value.GetType());
                Assert.AreEqual(0x3fc0000000000002, tc4.value);
                Assert.AreEqual(typeof(double), tc4.value2.GetType());
                Assert.AreEqual(0.0, tc4.value2);
                Assert.AreEqual(typeof(bool), tc4.value3.GetType());
                Assert.AreEqual(false, tc4.value3);
            }
        }

        private class TestClass5
        {
            public int flag;

            [TypeChoice("flag", TypeChoiceComparer.Equal, 0x00000001, typeof(TestClass51))]
            [TypeChoice("flag", TypeChoiceComparer.Equal, 0x00000002, typeof(TestClass52))]
            public IChoiceInherit inherit;
        }
        private interface IChoiceInherit { }
        private class TestClass51 : IChoiceInherit
        {
            public int value51;
        }
        private class TestClass52 : IChoiceInherit
        {
            public long value52;
        }

        [TestMethod]
        public void TypeChoiceInheritanceRead()
        {
            var input = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00
            };
            var ms = new MemoryStream(input);

            using (var br = new BinaryReaderX(ms))
            {
                var tc5 = br.ReadType<TestClass5>();

                Assert.AreEqual(typeof(TestClass51), tc5.inherit.GetType());
                Assert.AreEqual(2, (tc5.inherit as TestClass51).value51);

                ms.Position = 0;
                ms.Write(new byte[] { 0x02 }, 0, 1);
                ms.Position = 0;

                tc5 = br.ReadType<TestClass5>();

                Assert.AreEqual(typeof(TestClass52), tc5.inherit.GetType());
                Assert.AreEqual(0x0000000300000002, (tc5.inherit as TestClass52).value52);
            }
        }
    }
}
