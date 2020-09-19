//using System.Collections.Generic;
//using System.Drawing;
//using Kanvas.Encoding;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Linq;
//using Kontract.Models.IO;

//namespace KanvasUnitTests
//{
//    [TestClass]
//    public class RgbaTests
//    {
//        [TestMethod]
//        public void Rgba8888Le()
//        {
//            var rgbaFormat = new RGBA(8, 8, 8, 8);
//            var content = new byte[] { 0xff, 0xaa, 0x55, 0x00 };

//            var colorList = rgbaFormat.Load(content).ToList();

//            Assert.AreEqual(1, colorList.Count);
//            Assert.AreEqual(0x00, colorList[0].R);
//            Assert.AreEqual(0x55, colorList[0].G);
//            Assert.AreEqual(0xaa, colorList[0].B);
//            Assert.AreEqual(0xff, colorList[0].A);
//        }

//        [TestMethod]
//        public void Rgba8888Be()
//        {
//            var rgbaFormat = new RGBA(8, 8, 8, 8, "RGBA", ByteOrder.BigEndian);
//            var content = new byte[] { 0x00, 0x55, 0xaa, 0xff };

//            var colorList = rgbaFormat.Load(content).ToList();

//            Assert.AreEqual(1, colorList.Count);
//            Assert.AreEqual(0x00, colorList[0].R);
//            Assert.AreEqual(0x55, colorList[0].G);
//            Assert.AreEqual(0xaa, colorList[0].B);
//            Assert.AreEqual(0xff, colorList[0].A);
//        }

//        [TestMethod]
//        public void Rgba8888Le_SwitchedComponents()
//        {
//            var rgbaFormatAlphaFirst = new RGBA(8, 8, 8, 8, "ARGB");
//            var rgbaFormatSwapColor = new RGBA(8, 8, 8, 8, "BGRA");
//            var rgbaFormatBoth = new RGBA(8, 8, 8, 8, "ABGR");
//            var content = new byte[] { 0xff, 0xaa, 0x55, 0x00 };

//            var colorListAlphaFirst = rgbaFormatAlphaFirst.Load(content).ToList();
//            var colorListColorSwap = rgbaFormatSwapColor.Load(content).ToList();
//            var colorListBoth = rgbaFormatBoth.Load(content).ToList();

//            Assert.AreEqual(1, colorListAlphaFirst.Count);
//            Assert.AreEqual(0x00, colorListAlphaFirst[0].A);
//            Assert.AreEqual(0x55, colorListAlphaFirst[0].R);
//            Assert.AreEqual(0xaa, colorListAlphaFirst[0].G);
//            Assert.AreEqual(0xff, colorListAlphaFirst[0].B);

//            Assert.AreEqual(1, colorListColorSwap.Count);
//            Assert.AreEqual(0x00, colorListColorSwap[0].B);
//            Assert.AreEqual(0x55, colorListColorSwap[0].G);
//            Assert.AreEqual(0xaa, colorListColorSwap[0].R);
//            Assert.AreEqual(0xff, colorListColorSwap[0].A);

//            Assert.AreEqual(1, colorListBoth.Count);
//            Assert.AreEqual(0x00, colorListBoth[0].A);
//            Assert.AreEqual(0x55, colorListBoth[0].B);
//            Assert.AreEqual(0xaa, colorListBoth[0].G);
//            Assert.AreEqual(0xff, colorListBoth[0].R);
//        }

//        [TestMethod]
//        public void Rgba8888Be_SwitchedComponents()
//        {
//            var rgbaFormatAlphaFirst = new RGBA(8, 8, 8, 8, "ARGB", ByteOrder.BigEndian);
//            var rgbaFormatSwapColor = new RGBA(8, 8, 8, 8, "BGRA", ByteOrder.BigEndian);
//            var rgbaFormatBoth = new RGBA(8, 8, 8, 8, "ABGR", ByteOrder.BigEndian);
//            var content = new byte[] { 0x00, 0x55, 0xaa, 0xff };

//            var colorListAlphaFirst = rgbaFormatAlphaFirst.Load(content).ToList();
//            var colorListColorSwap = rgbaFormatSwapColor.Load(content).ToList();
//            var colorListBoth = rgbaFormatBoth.Load(content).ToList();

//            Assert.AreEqual(1, colorListAlphaFirst.Count);
//            Assert.AreEqual(0x00, colorListAlphaFirst[0].A);
//            Assert.AreEqual(0x55, colorListAlphaFirst[0].R);
//            Assert.AreEqual(0xaa, colorListAlphaFirst[0].G);
//            Assert.AreEqual(0xff, colorListAlphaFirst[0].B);

//            Assert.AreEqual(1, colorListColorSwap.Count);
//            Assert.AreEqual(0x00, colorListColorSwap[0].B);
//            Assert.AreEqual(0x55, colorListColorSwap[0].G);
//            Assert.AreEqual(0xaa, colorListColorSwap[0].R);
//            Assert.AreEqual(0xff, colorListColorSwap[0].A);

//            Assert.AreEqual(1, colorListBoth.Count);
//            Assert.AreEqual(0x00, colorListBoth[0].A);
//            Assert.AreEqual(0x55, colorListBoth[0].B);
//            Assert.AreEqual(0xaa, colorListBoth[0].G);
//            Assert.AreEqual(0xff, colorListBoth[0].R);
//        }

//        [TestMethod]
//        public void Rgb888()
//        {
//            var rgbaFormat = new RGBA(8, 8, 8, "RGB");
//            var content = new byte[] { 0xaa, 0x55, 0x00 };

//            var colorList = rgbaFormat.Load(content).ToList();

//            Assert.AreEqual(1, colorList.Count);
//            Assert.AreEqual(0xaa, colorList[0].R);
//            Assert.AreEqual(0x55, colorList[0].G);
//            Assert.AreEqual(0x00, colorList[0].B);
//            Assert.AreEqual(0xff, colorList[0].A);
//        }

//        [TestMethod]
//        public void Bgr888()
//        {
//            var rgbaFormatSwapColor = new RGBA(8, 8, 8, "BGR");
//            var content = new byte[] { 0xaa, 0x55, 0x00 };

//            var colorListColorSwap = rgbaFormatSwapColor.Load(content).ToList();

//            Assert.AreEqual(1, colorListColorSwap.Count);
//            Assert.AreEqual(0x00, colorListColorSwap[0].R);
//            Assert.AreEqual(0x55, colorListColorSwap[0].G);
//            Assert.AreEqual(0xaa, colorListColorSwap[0].B);
//            Assert.AreEqual(0xff, colorListColorSwap[0].A);
//        }

//        [TestMethod]
//        public void Rgba5551()
//        {
//            var rgbaFormat = new RGBA(5, 5, 5, 1);
//            var content = new byte[] { 0x3E, 0xF8 };

//            var colorList = rgbaFormat.Load(content).ToList();

//            Assert.AreEqual(1, colorList.Count);
//            Assert.AreEqual(0xff, colorList[0].R);
//            Assert.AreEqual(0x00, colorList[0].G);
//            Assert.AreEqual(0xff, colorList[0].B);
//            Assert.AreEqual(0x00, colorList[0].A);
//        }
//    }
//}
