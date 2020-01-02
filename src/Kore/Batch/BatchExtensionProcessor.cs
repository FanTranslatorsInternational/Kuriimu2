using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kore.Batch
{
    public class BatchExtensionProcessor<TExtension, TResult>
    {
        private readonly Func<TExtension, string, TResult> _processAction;

        public int TaskCount { get; set; } = Environment.ProcessorCount;

        public BatchExtensionProcessor(Func<TExtension, string, TResult> processAction)
        {
            _processAction = processAction;
        }

        public IList<(string path, TResult result)> Process(string path, bool isDirectory, bool searchSubDirectories, TExtension extensionType)
        {
            IEnumerable<string> files = isDirectory ?
                CollectFiles(path, searchSubDirectories) :
                new List<string> { path };

            return files.AsParallel()
                .WithDegreeOfParallelism(TaskCount)
                .Select(x => (x, _processAction(extensionType, x)))
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
    }
}
