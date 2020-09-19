using System.Buffers.Binary;
using Kryptography.Hash.Crc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KryptographyUnitTests
{
    [TestClass]
    public class Crc16Tests
    {
        private static readonly int _x25Result = 0x906E;

        private static readonly byte[] _check = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };

        [TestMethod]
        public void X25Compute_Successful()
        {
            var crc = Crc16.Create(Crc16Formula.X25);
            var hash = BinaryPrimitives.ReadInt16BigEndian(crc.Compute(_check)) & 0xFFFF;

            Assert.AreEqual(_x25Result, hash);
        }
    }
}
