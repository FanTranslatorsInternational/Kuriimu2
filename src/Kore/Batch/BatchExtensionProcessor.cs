using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Kore.Batch
{
    public class BatchExtensionProcessor<TExtension, TResult>
    {
        private readonly Func<TExtension, string, TResult> _processAction;
        private readonly ILogger _log;

        public int TaskCount { get; set; } = Environment.ProcessorCount;

        public BatchExtensionProcessor(Func<TExtension, string, TResult> processAction, ILogger log)
        {
            _processAction = processAction;
            _log = log;
        }

        public async Task<IList<(string path, TResult result)>> Process(string path, bool isDirectory, bool searchSubDirectories, TExtension extensionType)
        {
            var files = isDirectory ?
                CollectFiles(path, searchSubDirectories).ToList() :
                new List<string> { path };

            return await Task.Run(() => files.AsParallel()
                .WithDegreeOfParallelism(TaskCount)
                .Select(x => (x, ExecuteProcessDelegate(extensionType, x)))
                .ToList());
        }

        private IEnumerable<string> CollectFiles(string path, bool searchSubDirectories)
        {
            if (searchSubDirectories)
                foreach (var directory in Directory.EnumerateDirectories(path))
                    foreach (var file in CollectFiles(directory, true))
                        yield return file;

            foreach (var file in Directory.EnumerateFiles(path))
                yield return file;
        }

        private TResult ExecuteProcessDelegate(TExtension extensionType, string filePath)
        {
            _log.Information("Process file '{0}'.", filePath);
            return _processAction(extensionType, filePath);
        }
    }
}
