using System;
using Kontract.Interfaces;
using Kontract.Models;

namespace Kore.Batch.Processors
{
    public interface IBatchProcessor
    {
        void Process(ProcessElement processElement, IKuriimuProgress progress);
    }
}
