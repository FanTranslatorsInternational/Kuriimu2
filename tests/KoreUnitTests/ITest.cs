using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces;

namespace KoreUnitTests
{
    public interface ITest:IPlugin
    {
        List<string> Communication { get; set; }
    }
}
