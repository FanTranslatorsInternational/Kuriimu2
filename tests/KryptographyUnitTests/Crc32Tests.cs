using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Kryptography.Hash.Crc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KryptographyUnitTests
{
    [TestClass]
    public class Crc32Tests
    {
        private const uint DefaultPolynomial = 0x04C11DB7;
        private const uint DefaultReflectedPolynomial = 0xEDB88320;

        private const uint HashResult = 0xCBF43926;

        private static readonly byte[] _check = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };

        [TestMethod]
        public void DefaultPolynomial_IsCorrect()
        {
            Assert.AreEqual(DefaultPolynomial, Crc32.DefaultPolynomial);
        }

        [TestMethod]
        public void DefaultReflectedPolynomial_IsCorrect()
        {
            Assert.AreEqual(DefaultReflectedPolynomial, Crc32.DefaultReflectedPolynomial);
        }

        [TestMethod]
        public void NormalDefaultCompute()
        {
            var crc = Crc32.Create(Crc32Formula.Normal);
            var hash = crc.Compute(_check);

            Assert.AreEqual(HashResult, BinaryPrimitives.ReadUInt32BigEndian(hash));
        }

        [TestMethod]
        public void ReflectedDefaultCompute()
        {
            var crc = Crc32.Create(Crc32Formula.Reflected);
            var hash = crc.Compute(_check);

            Assert.AreEqual(HashResult, BinaryPrimitives.ReadUInt32BigEndian(hash));
        }

        [TestMethod]
        public void NormalComputeWithPolynomial()
        {
            var crc = Crc32.Create(Crc32Formula.Normal, DefaultPolynomial);
            var hash = crc.Compute(_check);

            Assert.AreEqual(HashResult, BinaryPrimitives.ReadUInt32BigEndian(hash));
        }

        [TestMethod]
        public void ReflectedComputeWithPolynomial()
        {
            var crc = Crc32.Create(Crc32Formula.Reflected, DefaultReflectedPolynomial);
            var hash = crc.Compute(_check);

            Assert.AreEqual(HashResult, BinaryPrimitives.ReadUInt32BigEndian(hash));
        }
    }
}
