using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO.Attributes;

namespace WinFormsTest.Image.Models
{
    [Alignment(0x10)]
    class Header
    {
        [FixedLength(8)]
        public string magic="IMG TEST";
        public int dataLength;
        public int format;
        public int width;
        public int height;
    }
}
