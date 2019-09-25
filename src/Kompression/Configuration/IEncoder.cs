using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Configuration
{
    public interface IEncoder
    {
        void Encode(Stream input, Stream output);
    }
}
