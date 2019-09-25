using System;
using Kontract.Models;

namespace Kore.Batch.Processors
{
    public interface IBatchHashProcessor
    {
        BatchHashResult Process(string inputFile, IProgress<ProgressReport> progress);
    }
}
