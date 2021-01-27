using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kontract.Models.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KanvasUnitTests
{
    [TestClass]
    public class IndexTests
    {
        [TestMethod]
        public void Index_I4()
        {
            var enc=new Index(4, ByteOrder.LittleEndian);
            var c=enc.Load(new byte[] {0xF7},
                new[]
                {
                    Color.Empty, Color.Empty, Color.Empty, Color.Empty, Color.Empty, Color.Empty, Color.Empty,
                    Color.Black, Color.Empty, Color.Empty, Color.Empty, Color.Empty, Color.Empty, Color.Empty,
                    Color.Empty, Color.Wheat
                }, 1).ToArray();

            Assert.AreEqual(Color.Wheat, c[0]);
            Assert.AreEqual(Color.Black, c[1]);
        }
    }
}
