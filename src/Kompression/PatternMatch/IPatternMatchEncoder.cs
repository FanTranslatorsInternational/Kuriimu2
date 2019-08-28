using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.PatternMatch
{
    public interface IPatternMatchEncoder : IDisposable
    {
        void Encode(Stream input, Stream output, Match[] matches);
    }
}
