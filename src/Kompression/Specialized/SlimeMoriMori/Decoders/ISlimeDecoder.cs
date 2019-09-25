using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    interface ISlimeDecoder
    {
        void Decode(Stream input, Stream output);
    }
}
