using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO.Attributes;

namespace WinFormsTest.Image.Models
{
    [Alignment(0x10)]
    class IndexHeader
    {
        [FixedLength(8)]
        public string magic = "IIMGTEST";
        public int paletteLength;
        public int colorCount;
        public int dataLength;
        public int paletteFormat;
        public int imageFormat;
        public int width;
        public int height;
    }
}
