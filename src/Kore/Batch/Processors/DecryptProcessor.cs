using System;
using System.IO;
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

        public async void Process(ProcessElement processElement, IProgress<ProgressReport> progress)
        {
            var read = File.OpenRead(processElement.InputFilename);
            var write = File.Create(processElement.OutputFilename);

            await _cipherAdapter.Decrypt(read, write, progress);

            read.Close();
            write.Close();
        }
    }
}
