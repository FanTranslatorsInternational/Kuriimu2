using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore.Batch
{
    public class ProcessElement
    {
        public string InputFilename { get; }
        public string OutputFilename { get; }

        public ProcessElement(string inputFilename, string outputFilename)
        {
            InputFilename = inputFilename;
            OutputFilename = outputFilename;
        }
    }
}
