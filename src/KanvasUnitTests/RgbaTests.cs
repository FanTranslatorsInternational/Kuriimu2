using Kanvas.Format;
using Komponent.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace KanvasUnitTests
{
    [TestClass]
    public class RgbaTests
    {
        [TestMethod]
        public void Rgba8888Le()
        {
            var rgbaFormat = new RGBA(8, 8, 8, 8);
            var content = new byte[] { 0xff, 0xaa, 0x55, 0x00 };

            var colorList = rgbaFormat.Load(content).ToList();

            Assert.AreEqual(1, colorList.Count);
            Assert.AreEqual(0x00, colorList[0].R);
            Assert.AreEqual(0x55, colorList[0].G);
            Assert.AreEqual(0xaa, colorList[0].B);
            Assert.AreEqual(0xff, colorList[0].A);
        }

        [TestMethod]
        public void Rgba8888Be()
        {
            var rgbaFormat = new RGBA(8, 8, 8, 8) { ByteOrder = ByteOrder.BigEndian };
            var content = new byte[] { 0x00, 0x55, 0xaa, 0xff };

            var colorList = rgbaFormat.Load(content).ToList();

            Assert.AreEqual(1, colorList.Count);
            Assert.AreEqual(0x00, colorList[0].R);
            Assert.AreEqual(0x55, colorList[0].G);
            Assert.AreEqual(0xaa, colorList[0].B);
            Assert.AreEqual(0xff, colorList[0].A);
        }
    }
}
