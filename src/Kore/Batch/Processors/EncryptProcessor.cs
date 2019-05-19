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
    public class EncryptProcessor : IBatchProcessor
    {
        private readonly ICipherAdapter _cipherAdapter;

        public EncryptProcessor(ICipherAdapter cipherAdapter)
        {
            _cipherAdapter = cipherAdapter ?? throw new ArgumentNullException(nameof(cipherAdapter));
        }

        public void Process(ProcessElement processElement, IProgress<ProgressReport> progress)
        {
            _cipherAdapter.Encrypt(File.OpenRead(processElement.InputFilename),
                File.OpenWrite(processElement.OutputFilename), progress);
        }
    }
}
