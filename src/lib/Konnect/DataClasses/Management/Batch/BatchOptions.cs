using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Konnect.DataClasses.Management.Batch
{
    public class BatchOptions
    {
        public required bool SubDirectories { get; init; }

        public BatchTextOptions? TextOptions { get; set; }
    }
}
