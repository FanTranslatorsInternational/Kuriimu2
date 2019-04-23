using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Models;

namespace Kanvas.Quantization.Helper
{
    internal static class ParallelProcessing
    {
        public static void ProcessList<TInput, TOutput>(TInput[] input, TOutput output, Action<TaskModel<TInput[], TOutput>> processingAction, int taskCount)
        {
            var elementCount = input.Length / taskCount;
            var overflow = input.Length - elementCount * taskCount;

            var tasks = new TaskModel<TInput[], TOutput>[taskCount];
            for (int i = 0; i < taskCount; i++)
                tasks[i] = new TaskModel<TInput[], TOutput>(input, output, i * elementCount, elementCount + (i == taskCount - 1 ? overflow : 0));

            Parallel.ForEach(tasks, processingAction);
        }
    }
}
