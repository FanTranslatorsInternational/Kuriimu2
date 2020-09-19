using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KanvasUnitTests
{
    [TestClass]
    public class AstcTests
    {
        [TestMethod]
        public void Astc_SinglePartition_Decoding_Successful()
        {
            var block = new byte[]
            {
                0b01000010, 0b00000000, 0b00000001, 0b11111110,
                0b11111111, 0b00000001, 0b00000000, 0b11111110,
                0b00000001, 0b00000000, 0b00000000, 0b00000000,
                0b00011011, 0b01101100, 0b10110001, 0b11000110
            };
            var astcEncoder = new ASTC(4, 4, 1);
            var colors = astcEncoder.Load(block, 8).ToArray();
        }

        [TestMethod]
        public void Astc_MultiPartition_Decoding_Successful()
        {
            var block = new byte[]
            {
                0b01000010, 0b00000000, 0b00000001, 0b11111110,
                0b11111111, 0b00000001, 0b00000000, 0b11111110,
                0b00000001, 0b00000000, 0b00000000, 0b00000000,
                0b00011011, 0b01101100, 0b10110001, 0b11000110
            };
            var astcEncoder = new ASTC(4, 4, 1);
            var colors = astcEncoder.Load(block, 8).ToArray();
        }
    }
}
