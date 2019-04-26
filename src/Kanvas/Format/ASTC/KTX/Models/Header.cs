using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Format.ASTC.KTX.Models
{
    internal class Header
    {
        public string magic;
        public int endian;
        public GLDataType glType;
        public int glTypeSize;
        public GLFormat glFormat;
        public GLFormat glInternalFormat;
        public GLFormat glBaseInternalFormat;
        public int width;
        public int height;
        public int depth;
        public int arrayCount;
        public int faceCount;
        public int mipmapCount;
        public int keyValueDataSize;
    }
}
