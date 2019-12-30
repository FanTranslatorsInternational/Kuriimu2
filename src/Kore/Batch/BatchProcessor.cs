using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces;
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

        public IList<BatchErrorReport<ProcessElement>> ErrorReports { get; private set; }

        public Task Process(string inputDirectory, string outputDirectory, IBatchProcessor processor, IKuriimuProgress progress)
        {
            ErrorReports = new List<BatchErrorReport<ProcessElement>>();
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

        private Task ProcessParallel(IEnumerable<ProcessElement> toProcess, IKuriimuProgress progress, Action<ProcessElement, IKuriimuProgress> taskDelegate)
        {
            var activeTasks = new (Task task, ProcessElement element)?[TaskCount];
            var enumerator = toProcess.GetEnumerator();
            var moveResult = enumerator.MoveNext();
            if (!moveResult)
                return Task.CompletedTask;

            while (moveResult || activeTasks.Any(x => x != null))
            {
                for (int i = 0; i < TaskCount; i++)
                {
                    if (activeTasks[i]?.task?.IsCompleted ?? false)
                    {
                        if (activeTasks[i]?.task?.IsFaulted ?? false)
                            ErrorReports.Add(new BatchErrorReport<ProcessElement>(activeTasks[i]?.element, activeTasks[i]?.task.Exception));

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
                    var task = new Task(() => taskDelegate(element, progress));

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
            return Task.CompletedTask;
        }
    }
}
