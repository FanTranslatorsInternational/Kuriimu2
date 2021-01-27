using Komponent.IO;
using Komponent.IO.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Models.IO;

namespace KomponentUnitTests
{
    [TestClass]
    public class BinaryWriterXTests
    {
        [TestMethod]
        public void BaseWrites()
        {
            var expect = new byte[] {
                0x00, 0x11, 0x21, 0x22, 0x32, 0x33, 0x43, 0x44,
                0x44, 0x44, 0x54, 0x55, 0x55, 0x55, 0x01, 0x65,
                0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x76,
                0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0xf8,
                0x53, 0xe3, 0x3d, 0xd1, 0x22, 0xdb, 0xf9, 0x7e,
                0x6a, 0xbc, 0x3f, 0x3f, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00
            };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms))
            {
                bw.Write((byte)0x00);
                bw.Write((sbyte)0x11);
                bw.Write((short)0x2221);
                bw.Write((ushort)0x3332);
                bw.Write(0x44444443);
                bw.Write(0x55555554u);
                bw.Write(true);
                bw.Write(0x6666666666666665);
                bw.Write(0x7777777777777776u);

                bw.Write(0.111f);
                bw.Write(0.111);
                bw.Write((decimal)63.0);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
            }
        }

        [TestMethod]
        public void CharStringWrites()
        {
            var expect = new byte[] {
                0x33, 0x34, 0x34, 0x02, 0x35, 0x35, 0x06, 0x36,
                0x00, 0x36, 0x00, 0x00, 0x00, 0x03, 0x37, 0x37,
                0x00, 0x38, 0x38, 0x00, 0x02, 0x38, 0x38, 0x38,
                0x38
            };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms))
            {
                bw.Write('3');
                bw.Write(new char[] { '4', '4' });

                bw.Write("55");
                bw.WriteString("66", Encoding.Unicode);
                bw.WriteString("77", Encoding.GetEncoding("SJIS"));

                bw.WriteString("88", Encoding.ASCII, false, true);
                bw.WriteString("88", Encoding.ASCII, true, false);
                bw.WriteString("88", Encoding.ASCII, false, false);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
            }
        }

        [TestMethod]
        public void AlignmentWrite()
        {
            var expect = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms))
            {
                bw.Write((byte)0x01);
                bw.WriteAlignment(0x08, 0x00);
                bw.Write((byte)0x02);
                bw.WriteAlignment(0x10, 0x00);
                bw.Write((byte)0x03);
                bw.WritePadding(7);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
            }
        }

        [TestMethod]
        public void BitWriting()
        {
            var expect = new byte[] {
                0x00, 0x80, 0x0F, 0xF8
            };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms, true, ByteOrder.LittleEndian, BitOrder.MostSignificantBitFirst, 2))
            {
                bw.WriteBit(true);
                bw.Flush();

                bw.WriteBits(0x1F, 5);
                bw.WriteBits(0x00, 6);
                bw.WriteBits(0x0F, 5);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
            }

            var expect2 = new byte[] {
                0x00, 0x80, 0x00, 0x01
            };
            var ms2 = new MemoryStream();
            using (var bw = new BinaryWriterX(ms2, ByteOrder.LittleEndian, BitOrder.LowestAddressFirst, 2))
            {
                bw.WriteBit(false);
                bw.WriteBits(0, 14);
                bw.WriteBit(true);

                bw.ByteOrder = ByteOrder.BigEndian;
                bw.WriteBit(false);
                bw.WriteBits(0, 14);
                bw.WriteBit(true);

                Assert.IsTrue(ms2.ToArray().SequenceEqual(expect2));
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
        public void GenericWriting()
        {
            var expect = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x04, 0xF8, 0x0F, 0x11,
                0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0x34, 0x34,
                0x34, 0x35, 0x36, 0x34, 0x05, 0x00, 0x00, 0x00,
                0xFC,
                0x01, 0x00, 0x00, 0x00, 0x04, 0xF8, 0x10, 0x11,
                0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0x34, 0x34,
                0x34, 0x35, 0x36, 0x34, 0x05, 0x00, 0x00, 0x00,
                0xFC,
                0x01, 0x00, 0x00, 0x00, 0x04, 0xF8, 0x11, 0x11,
                0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0x34, 0x34,
                0x34, 0x35, 0x36, 0x34, 0x05, 0x00, 0x00, 0x00,
                0xFC
            };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms))
            {
                var examp = new TestClass
                {
                    exp0 = true,
                    exp1 = 0x04,
                    exp2 = new TestClass.TestClass2
                    {
                        val1 = 0x0F,
                        val2 = 0x00,
                        val3 = 0x1F
                    },
                    exp3 = new byte[] { 0x11, 0x11, 0x11 },
                    exp4 = new List<byte> { 0x22, 0x22, 0x22, 0x22 },
                    exp5 = "444564",
                    exp6 = new TestClass.TestClass3
                    {
                        val1 = 0x05
                    },
                    exp7 = 0xFF
                };

                bw.WriteType(examp);

                examp.exp2.val1++;
                bw.WriteType(examp);
                examp.exp2.val1++;
                bw.WriteType(examp);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
            }
        }

        [Alignment(8)]
        private class TestClass1
        {
            public int var0;
        }

        [TestMethod]
        public void AlignmentAttributeWrite()
        {
            var expect = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms))
            {
                var tc1 = new TestClass1 { var0 = 1 };
                bw.WriteType(tc1);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
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
            var expect = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x03,
                0x00
            };
            var ms = new MemoryStream();

            using (var bw = new BinaryWriterX(ms))
            {
                var tc2 = new TestClass2
                {
                    var0 = 1,
                    var1 = new TestClass2.TestClass2_2
                    {
                        var2 = 2
                    },
                    var3 = new byte[2],
                    var4 = new TestClass2.TestClass2_3
                    {
                        var0 = 3,
                        var5 = new byte[1]
                    }
                };
                bw.WriteType(tc2);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
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
            var expect = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x02, 0x02
            };
            var ms = new MemoryStream();

            using (var br = new BinaryWriterX(ms))
            {
                var tc3 = new TestClass3
                {
                    var0 = new TestClass3.TestClass31
                    {
                        var0 = 1,
                        var1 = 2
                    },
                    var1 = new byte[] { 2, 2 }
                };
                br.WriteType(tc3);

                Assert.IsTrue(ms.ToArray().SequenceEqual(expect));
            }
        }
    }
}
