using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konnect.Contract.Plugin.Game;

namespace Konnect.DataClasses.Management.Batch
{
    public class BatchTextOptions
    {
        public required TextFormat Format { get; init; }

        public IGamePlugin? Preview { get; init; }
    }
}
