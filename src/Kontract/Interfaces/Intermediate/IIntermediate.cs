using Kontract.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Intermediate
{
    public interface IIntermediate : IPlugin
    {
        /// <summary>
        /// The name of the Intermediate Adapter
        /// </summary>
        string Name { get; }
    }
}
