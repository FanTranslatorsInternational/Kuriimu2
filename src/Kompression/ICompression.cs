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
        Stream Decompress(Stream input);
        Stream Compress(Stream input);
    }
}
