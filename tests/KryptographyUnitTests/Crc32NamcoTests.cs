using System;
using System.Collections.Generic;
using System.Text;
using Kryptography.Hash.Crc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KryptographyUnitTests
{
    [TestClass]
    public class Crc32NamcoTests
    {
        private const uint DefaultReflectedPolynomial = 0xEDB88320;

        private const uint HashResult = 0x93718920;

        private static readonly byte[] _check = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };

        [TestMethod]
        public void DefaultReflectedPolynomial_IsCorrect()
        {
            Assert.AreEqual(DefaultReflectedPolynomial, Crc32.DefaultReflectedPolynomial);
        }

        [TestMethod]
        public void ReflectedDefaultCompute()
        {
            var crc = Crc32Namco.Create();
            var hash = crc.Compute(_check);

            Assert.AreEqual(HashResult, hash);
        }

        [TestMethod]
        public void ReflectedComputeWithPolynomial()
        {
            var crc = Crc32Namco.Create(DefaultReflectedPolynomial);
            var hash = crc.Compute(_check);

            Assert.AreEqual(HashResult, hash);
        }
    }
}
