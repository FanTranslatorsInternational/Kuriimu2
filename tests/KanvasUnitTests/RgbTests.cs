using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KanvasUnitTests
{
    [TestClass]
    public class RgbTests
    {
        [TestMethod]
        public void Rgba888Le()
        {
            var rgbaFormat = new RGBA(8, 8, 8);
            var content = new byte[] { 0xaa, 0x55, 0x00 };

            var colorList = rgbaFormat.Load(content).ToList();

            Assert.AreEqual(1, colorList.Count);
            Assert.AreEqual(0x00, colorList[0].R);
            Assert.AreEqual(0x55, colorList[0].G);
            Assert.AreEqual(0xaa, colorList[0].B);
            Assert.AreEqual(0xff, colorList[0].A);
        }

        [TestMethod]
        public void Rgba888Be()
        {
            var rgbaFormat = new RGBA(8, 8, 8) { ByteOrder = ByteOrder.BigEndian };
            var content = new byte[] { 0x00, 0x55, 0xaa };

            var colorList = rgbaFormat.Load(content).ToList();

            Assert.AreEqual(1, colorList.Count);
            Assert.AreEqual(0x00, colorList[0].R);
            Assert.AreEqual(0x55, colorList[0].G);
            Assert.AreEqual(0xaa, colorList[0].B);
            Assert.AreEqual(0xff, colorList[0].A);
        }

        [TestMethod]
        public void Rgba888Le_otherSettings()
        {
            var rgbaFormatSwapColor = new RGBA(8, 8, 8) { ShouldSwapColorChannels = true };
            var content = new byte[] { 0xaa, 0x55, 0x00 };

            var colorListColorSwap = rgbaFormatSwapColor.Load(content).ToList();

            Assert.AreEqual(1, colorListColorSwap.Count);
            Assert.AreEqual(0x00, colorListColorSwap[0].B);
            Assert.AreEqual(0x55, colorListColorSwap[0].G);
            Assert.AreEqual(0xaa, colorListColorSwap[0].R);
            Assert.AreEqual(0xff, colorListColorSwap[0].A);
        }

        [TestMethod]
        public void Rgba888Be_otherSettings()
        {
            var rgbaFormatSwapColor = new RGBA(8, 8, 8, 8) { ByteOrder = ByteOrder.BigEndian, ShouldSwapColorChannels = true };
            var content = new byte[] { 0x00, 0x55, 0xaa, 0xff };

            var colorListColorSwap = rgbaFormatSwapColor.Load(content).ToList();

            Assert.AreEqual(1, colorListColorSwap.Count);
            Assert.AreEqual(0x00, colorListColorSwap[0].B);
            Assert.AreEqual(0x55, colorListColorSwap[0].G);
            Assert.AreEqual(0xaa, colorListColorSwap[0].R);
            Assert.AreEqual(0xff, colorListColorSwap[0].A);
        }
    }
}
