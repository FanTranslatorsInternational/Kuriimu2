using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kanvas
{
    static class ParallelProcessing
    {
        public static void ProcessParallel<TTask>(IEnumerable<TTask> tasks, int taskCount,
            Action<TTask> taskDelegate) where TTask : class
        {
            var enumerator = tasks.GetEnumerator();
            var taskBuffer = new Task[taskCount];

            // Fill active Tasks initially
            FillTasks(taskBuffer, enumerator, taskDelegate);

            while (taskBuffer.Any(x => x != null))
            {
                ClearCompletedTasks(taskBuffer);
                FillTasks(taskBuffer, enumerator, taskDelegate);
            }

            enumerator.Dispose();
        }

        private static void FillTasks<TTask>(Task[] tasks, IEnumerator<TTask> enumerator, Action<TTask> taskDelegate)
            where TTask : class
        {
            for (var i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null)
                    continue;

                var nextElement = RetrieveNextElement(enumerator);
                if (nextElement != null)
                {
                    tasks[i] = new Task(() => taskDelegate(nextElement));
                    tasks[i].Start();
                }
            }
        }

        private static void ClearCompletedTasks(Task[] tasks)
        {
            for (var i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] == null)
                    continue;

                if (tasks[i].IsCompleted)
                {
                    tasks[i].Dispose();
                    tasks[i] = null;
                }
            }
        }

        private static TElement RetrieveNextElement<TElement>(IEnumerator<TElement> enumerator)
            where TElement : class
        {
            if (!enumerator.MoveNext())
                return null;

            return enumerator.Current;
        }
    }
}
