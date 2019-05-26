using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KompressionUnitTests
{
    [TestClass]
    public class LempelZivTests
    {
        [TestMethod]
        public void FindOccurences_IsCorrect()
        {
            var input = new byte[]
                {0x00, 0x01, 0x02, 0x02, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x02, 0x00, 0x00, 0x00, 0x00};

            var results = Kompression.LempelZiv.Common.FindOccurrences(input, 8, 4, 16);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].Position);
            Assert.AreEqual(8, results[0].Length);
            Assert.AreEqual(7, results[0].Displacement);
        }
    }
}
