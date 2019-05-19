using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;

namespace Kore.Batch.Processors
{
    public class DecryptProcessor : IBatchProcessor
    {
        private readonly ICipherAdapter _cipherAdapter;

        public DecryptProcessor(ICipherAdapter cipherAdapter)
        {
            _cipherAdapter = cipherAdapter ?? throw new ArgumentNullException(nameof(cipherAdapter));
        }

        public void Process(ProcessElement processElement, IProgress<ProgressReport> progress)
        {
            _cipherAdapter.Decrypt(File.OpenRead(processElement.InputFilename),
                File.OpenWrite(processElement.OutputFilename), progress);
        }
    }
}
