using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Models;
using Kanvas.Quantization.Models.Parallel;

namespace Kanvas.Quantization.Helper
{
    internal static class ParallelProcessing
    {
        /// <summary>
        /// Processes a list in parallel after dividing it in taskCount even regions
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="processingAction"></param>
        /// <param name="taskCount"></param>
        public static void ProcessList<TInput, TOutput>(TInput[] input, TOutput output, Action<LineTask<TInput[], TOutput>> processingAction, int taskCount)
        {
            var elementCount = input.Length / taskCount;
            var overflow = input.Length - elementCount * taskCount;

            var tasks = new LineTask<TInput[], TOutput>[taskCount];
            for (int i = 0; i < taskCount; i++)
                tasks[i] = new LineTask<TInput[], TOutput>(input, output, i * elementCount, elementCount + (i == taskCount - 1 ? overflow : 0));

            Parallel.ForEach(tasks, processingAction);
        }

        /// <summary>
        /// Processes a list in parallel with a given element threshold per line
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="inputList"></param>
        /// <param name="output"></param>
        /// <param name="lineLength"></param>
        /// <param name="threshold"></param>
        /// <param name="taskCount"></param>
        /// <param name="taskDelegate"></param>
        public static void ProcessList<TInput, TOutput>(TInput[] inputList, TOutput output, int lineLength, int threshold, int taskCount, Action<DelayedLineTask<TInput, TOutput>, int> taskDelegate)
        {
            if (inputList.Length % lineLength > 0)
                throw new InvalidOperationException("Length of input list needs to be a multiple of lineLength.");

            // create line tasks
            var count = inputList.Length / lineLength;
            var lineTasks = new DelayedLineTask<TInput, TOutput>[count];
            DelayedLineTask<TInput, TOutput> preTask = null;
            for (int i = 0; i < count; i++)
            {
                var lineTask = new DelayedLineTask<TInput, TOutput>(inputList, output, i * lineLength, lineLength, threshold, preTask);
                lineTasks[i] = preTask = lineTask;
            }

            ProcessParallel(lineTasks, taskCount, taskDelegate);
        }

        private static void ProcessParallel<TInput, TOutput>(DelayedLineTask<TInput, TOutput>[] lineTasks, int taskCount,
            Action<DelayedLineTask<TInput, TOutput>, int> taskDelegate)
        {
            var activeTasks = new Task[taskCount];
            var nextLineTask = 0;

            while (nextLineTask < lineTasks.Length || activeTasks.Any(x => x != null))
            {
                for (int i = 0; i < taskCount; i++)
                {
                    if (activeTasks[i]?.IsCompleted ?? false)
                    {
                        activeTasks[i].Dispose();
                        activeTasks[i] = null;
                    }
                }

                var freeSlots = taskCount - activeTasks.Count(x => x != null);
                for (int i = 0; i < freeSlots; i++)
                {
                    if (nextLineTask >= lineTasks.Length)
                        break;

#if DEBUG
                    //Trace.WriteLine(nextLineTask);
#endif

                    var lineTask = lineTasks[nextLineTask++];
                    var task = new Task(() => lineTask.Process(taskDelegate));

                    int index = 0;
                    for (int j = 0; j < taskCount; j++)
                        if (activeTasks[j] == null)
                        {
                            index = j;
                            break;
                        }

                    activeTasks[index] = task;
                    task.Start();
                }
            }
        }
    }
}
