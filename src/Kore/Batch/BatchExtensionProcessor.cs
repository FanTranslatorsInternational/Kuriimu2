using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kore.Logging;

namespace Kore.Batch
{
    public class BatchExtensionProcessor<TExtension, TResult>
    {
        private readonly Func<TExtension, string, TResult> _processAction;
        private readonly IConcurrentLog _log;

        public int TaskCount { get; set; } = Environment.ProcessorCount;

        public BatchExtensionProcessor(Func<TExtension, string, TResult> processAction, IConcurrentLog log)
        {
            _processAction = processAction;
            _log = log;
        }

        public IList<(string path, TResult result)> Process(string path, bool isDirectory, bool searchSubDirectories, TExtension extensionType)
        {
            var files = isDirectory ?
                CollectFiles(path, searchSubDirectories).ToList() :
                new List<string> { path };

            return files.AsParallel()
                .WithDegreeOfParallelism(TaskCount)
                .Select(x => (x, ExecuteProcessDelegate(extensionType, x)))
                .ToList();
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
            _log.QueueMessage(LogLevel.Information, $"Process file '{filePath}'.");
            return _processAction(extensionType, filePath);
        }
    }
}
