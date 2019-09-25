using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;
using Kore.Batch.Processors;

namespace Kore.Batch
{
    /// <summary>
    /// <para>Processes a given directory in parallel with a given processor method.</para>
    /// <para>It allows processing on all <see cref="IIntermediate"/> adapters.</para>
    /// </summary>
    public class BatchHashProcessor
    {
        public bool SearchSubDirectories { get; set; }

        public int TaskCount { get; set; } = 8;

        public IList<BatchErrorReport<string>> ErrorReports { get; private set; }

        public Task<List<BatchHashResult>> Process(string inputDirectory, IBatchHashProcessor processor, IProgress<ProgressReport> progress)
        {
            ErrorReports = new List<BatchErrorReport<string>>();
            return ProcessParallel(EnumerateAllFiles(inputDirectory), progress, processor.Process);
        }

        private IEnumerable<string> EnumerateAllFiles(string inputDirectory)
        {
            foreach (var file in Directory.EnumerateFiles(inputDirectory))
                yield return file;

            if (!SearchSubDirectories)
                yield break;

            foreach (var dir in Directory.EnumerateDirectories(inputDirectory))
                foreach (var file in EnumerateAllFiles(dir))
                    yield return file;
        }

        private Task<List<BatchHashResult>> ProcessParallel(IEnumerable<string> toProcess, IProgress<ProgressReport> progress, Func<string, IProgress<ProgressReport>, BatchHashResult> taskDelegate)
        {
            return Task.Factory.StartNew(() =>
                ProcessParallelInternal(toProcess, progress, taskDelegate));
        }

        private List<BatchHashResult> ProcessParallelInternal(IEnumerable<string> toProcess, IProgress<ProgressReport> progress, Func<string, IProgress<ProgressReport>, BatchHashResult> taskDelegate)
        {
            var results = new List<BatchHashResult>();
            var activeTasks = new (Task<BatchHashResult> task, string element)?[TaskCount];
            var enumerator = toProcess.GetEnumerator();
            var moveResult = enumerator.MoveNext();
            if (!moveResult)
                return results;

            while (moveResult || activeTasks.Any(x => x != null))
            {
                for (int i = 0; i < TaskCount; i++)
                {
                    if (activeTasks[i]?.task?.IsCompleted ?? false)
                    {
                        if (activeTasks[i]?.task?.IsFaulted ?? false)
                            ErrorReports.Add(new BatchErrorReport<string>(activeTasks[i]?.element, activeTasks[i]?.task.Exception));
                        else
                            results.Add(new BatchHashResult(activeTasks[i]?.task.Result.IsSuccessful ?? false, activeTasks[i]?.task.Result.File, activeTasks[i]?.task.Result.Result));

                        activeTasks[i]?.task.Dispose();
                        activeTasks[i] = null;
                    }
                }

                var freeSlots = TaskCount - activeTasks.Count(x => x != null);
                for (int i = 0; i < freeSlots; i++)
                {
                    if (!moveResult)
                        break;

                    var element = enumerator.Current;
                    var task = new Task<BatchHashResult>(() => taskDelegate(element, progress));

                    int index = 0;
                    for (int j = 0; j < TaskCount; j++)
                        if (activeTasks[j] == null)
                        {
                            index = j;
                            break;
                        }

                    activeTasks[index] = (task, element);
                    task.Start();

                    moveResult = enumerator.MoveNext();
                }
            }

            enumerator.Dispose();
            return results;
        }
    }
}
