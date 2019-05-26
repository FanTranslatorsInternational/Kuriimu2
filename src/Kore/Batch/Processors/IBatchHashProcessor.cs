using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Models;

namespace Kore.Batch.Processors
{
    public interface IBatchHashProcessor
    {
        BatchHashResult Process(string inputFile, IProgress<ProgressReport> progress);
    }
}
