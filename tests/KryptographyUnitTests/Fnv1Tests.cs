using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Kryptography.Hash.Crc;
using Kryptography.Hash.Fnv;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KryptographyUnitTests
{
    [TestClass]
    public class Fnv1Tests
    {
        private const uint HashResult = 0x24148816;

        private static readonly byte[] _check = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };

        [TestMethod]
        public void DefaultCompute()
        {
            var fnv = Fnv1.Create();
            var hash = fnv.Compute(_check);

            Assert.AreEqual(HashResult, BinaryPrimitives.ReadUInt32BigEndian(hash));
        }
    }
}
