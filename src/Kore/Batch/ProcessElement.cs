using System;

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

        public override string ToString()
        {
            var msg = $"{Environment.NewLine}Input File: {InputFilename}";
            msg += $"{Environment.NewLine}Output File: {OutputFilename}";
            return msg;
        }
    }
}
