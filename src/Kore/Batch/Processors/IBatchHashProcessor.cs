using System;
using Kontract.Interfaces;
using Kontract.Models;

namespace Kore.Batch.Processors
{
    public interface IBatchHashProcessor
    {
        BatchHashResult Process(string inputFile, IKuriimuProgress progress);
    }
}
