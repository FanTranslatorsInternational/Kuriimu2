using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Konnect.DataClasses.Management.Batch
{
    public record BatchFileResult(string FilePath, IList<string> DialogOptions, BatchFileStatus Status);
}
