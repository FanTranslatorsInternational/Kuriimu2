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
    public class BatchProcessor
    {
        public bool SearchSubDirectories { get; set; }

        public int TaskCount { get; set; } = 8;

        public Task Process(string inputDirectory, string outputDirectory, IBatchProcessor processor, IProgress<ProgressReport> progress)
        {
            return ProcessParallel(EnumerateAllFiles(inputDirectory, outputDirectory), progress, processor.Process);
        }

        private IEnumerable<ProcessElement> EnumerateAllFiles(string inputDirectory, string outputDirectory)
        {
            foreach (var file in Directory.EnumerateFiles(inputDirectory))
                yield return new ProcessElement(file, file.Replace(inputDirectory, outputDirectory));

            if (!SearchSubDirectories)
                yield break;

            foreach (var dir in Directory.EnumerateDirectories(inputDirectory))
                foreach (var processElement in EnumerateAllFiles(dir, dir.Replace(inputDirectory, outputDirectory)))
                    yield return processElement;
        }

        private Task ProcessParallel(IEnumerable<ProcessElement> toProcess, IProgress<ProgressReport> progress, Action<ProcessElement, IProgress<ProgressReport>> taskDelegate)
        {
            var activeTasks = new Task[TaskCount];
            var enumerator = toProcess.GetEnumerator();

            while (enumerator.Current != null || activeTasks.Any(x => x != null))
            {
                for (int i = 0; i < TaskCount; i++)
                {
                    if (activeTasks[i]?.IsCompleted ?? false)
                    {
                        activeTasks[i].Dispose();
                        activeTasks[i] = null;
                    }
                }

                var freeSlots = TaskCount - activeTasks.Count(x => x != null);
                for (int i = 0; i < freeSlots; i++)
                {
                    if (enumerator.Current == null)
                        break;

                    enumerator.MoveNext();
                    var element = enumerator.Current;
                    var task = new Task(() => taskDelegate(element, progress));

                    int index = 0;
                    for (int j = 0; j < TaskCount; j++)
                        if (activeTasks[j] == null)
                        {
                            index = j;
                            break;
                        }

                    activeTasks[index] = task;
                    task.Start();
                }
            }

            enumerator.Dispose();
            return Task.CompletedTask;
        }
    }
}
