using System;
using Kontract.Models;

namespace Kore.Batch.Processors
{
    public interface IBatchProcessor
    {
        void Process(ProcessElement processElement, IProgress<ProgressReport> progress);
    }
}
