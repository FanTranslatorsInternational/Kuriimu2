using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression
{
    public interface ICompression
    {
        void Decompress(Stream input, Stream output);
        void Compress(Stream input, Stream output);
    }
}
