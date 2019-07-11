using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv.Matcher.Models;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Matcher
{
    public interface ILzMatcher : IDisposable
    {
        LzMatch[] FindMatches(Stream input);
    }
}
