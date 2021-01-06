using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Encoding.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KanvasUnitTests
{
    [TestClass]
    public class PixelEncodingTests
    {
        [TestMethod]
        public void MemoryConsumption_Low_Success()
        {
            // Assign
            var encoding = new Rgba(8, 0, 0);
            var input = Enumerable.Range(0, 1024 * 1024 * 32).Select(x => (byte)(x % 256)).ToArray();

            // Act
            var colors = encoding.Load(input, 1);

            // Assert
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Assert.IsTrue(colors.All(x => 
                x== Color.FromArgb(255, 3, 0, 0)));

            stopwatch.Stop();
        }
    }
}
