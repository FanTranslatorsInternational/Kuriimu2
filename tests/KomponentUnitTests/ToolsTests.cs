using Komponent.IO;
using Komponent.IO.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KomponentUnitTests
{
    [TestClass]
    public class ToolsTests
    {
        private class Test1
        {
            public bool var1;
            public sbyte var2;
            public byte var3;
            public short var4;
            public ushort var5;
            public int var6;
            public uint var7;
            public long var8;
            public ulong var9;
            public float var10;
            public double var11;
            public decimal var12;
        }

        [TestMethod]
        public void BaseMeasure()
        {
            Assert.AreEqual(59, Tools.MeasureType(typeof(Test1)));
        }

        private class Test2
        {
            public Test1 var1;
            public Test1 var2;
            public bool var3;
        }

        [TestMethod]
        public void NestedClassMeasure()
        {
            Assert.AreEqual(59 * 2 + 1, Tools.MeasureType(typeof(Test2)));
        }

        private class StringTest1
        {
            public string var1;
        }
        private class StringTest2
        {
            public int var0;
            [VariableLength("var0")]
            public string var1;
        }
        private class StringTest3
        {
            [FixedLength(3, StringEncoding = StringEncoding.SJIS)]
            public string var1;
        }
        private class StringTest4
        {
            [FixedLength(3)]
            public string var1;
        }
        private class StringTest5
        {
            [FixedLength(3, StringEncoding = StringEncoding.UTF32)]
            public string var1;
        }

        [TestMethod]
        public void StringMeasure()
        {
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(StringTest1)));
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(StringTest2)));
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(StringTest3)));
            Assert.AreEqual(3, Tools.MeasureType(typeof(StringTest4)));
            Assert.AreEqual(12, Tools.MeasureType(typeof(StringTest5)));
        }

        private class ListTest1
        {
            public byte[] var1;
        }
        private class ListTest2
        {
            public List<byte> var1;
        }
        private class ListTest3
        {
            public byte var0;
            [VariableLength("var0")]
            public byte[] var1;
        }
        private class ListTest4
        {
            public byte var0;
            [VariableLength("var0")]
            public List<byte> var1;
        }
        private class ListTest5
        {
            [FixedLength(3)]
            public byte[] var1;
        }
        private class ListTest6
        {
            [FixedLength(3)]
            public List<int> var1;
        }

        [TestMethod]
        public void ListMeasure()
        {
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(ListTest1)));
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(ListTest2)));
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(ListTest3)));
            Assert.ThrowsException<InvalidOperationException>(() => Tools.MeasureType(typeof(ListTest4)));
            Assert.AreEqual(3, Tools.MeasureType(typeof(ListTest5)));
            Assert.AreEqual(12, Tools.MeasureType(typeof(ListTest6)));
        }

        private enum TestEnum : int
        {
            Hello,
            World
        }
        private class EnumTest1
        {
            public TestEnum enum1;
            public TestEnum enum2;
        }

        [TestMethod]
        public void EnumMeasure()
        {
            Assert.AreEqual(8, Tools.MeasureType(typeof(EnumTest1)));
        }

        private class LimitedTest1
        {
            public int var0;
            public LimitedTest2 var1;
            public int var2;
        }
        private class LimitedTest2
        {
            public int var0;
            public int var1;
            public int var2;
        }

        [TestMethod]
        public void LimitedMeasure()
        {
            Assert.AreEqual(12, Tools.MeasureTypeUntil(typeof(LimitedTest1), "var1.var2"));
        }
    }
}
