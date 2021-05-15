using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Models.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KomponentUnitTests.ByteReader
{
    [TestClass]
    public class BitReadingTests
    {
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
    }
}
