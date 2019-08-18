using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    interface ISlimeEncoder
    {
        void Encode(Stream input, Stream output, LzMatch[] matches);
    }
}
