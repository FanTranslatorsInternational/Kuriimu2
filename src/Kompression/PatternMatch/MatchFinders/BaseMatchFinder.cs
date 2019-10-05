using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.Models;
using Kompression.PatternMatch.MatchFinders.Parallel;

namespace Kompression.PatternMatch.MatchFinders
{
    public abstract class BaseMatchFinder : IMatchFinder
    {
        public FindLimitations FindLimitations { get; private set; }
        public FindOptions FindOptions { get; private set; }

        protected BaseMatchFinder(FindLimitations limits, FindOptions options)
        {
            this.FindLimitations = limits;
            FindOptions = options;
        }

        /// <inheritdoc cref="FindMatchesAtPosition(byte[],int)"/>
        public abstract IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position);

        /// <inheritdoc cref="GetAllMatches(byte[],int)"/>
        public IEnumerable<Match[]> GetAllMatches(byte[] input, int position)
        {
            SetupMatchFinder(input, position);
            var enumerators = SetupMatchFinderEnumerators(input, position);

            var taskCount = FindOptions.TaskCount;
            var tasks = new Task<bool>[taskCount];

            // Execute all tasks until end of file
            var continueExecution = true;
            while (continueExecution)
            {
                for (var i = 0; i < taskCount; i++)
                {
                    var enumerator = enumerators[i];
                    tasks[i] = new Task<bool>(() => enumerator.MoveNext());
                    tasks[i].Start();
                }

                Task.WaitAll(tasks);
                continueExecution = tasks.All(x => x.Result);

                for (var i = 0; i < taskCount; i++)
                    if (tasks[i].Result)
                        yield return enumerators[i].Current;
            }
        }

        /// <summary>
        /// Setup objects for use in <see cref="GetAllMatches(byte[],int)"/>.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="startPosition">The position to start at.</param>
        protected abstract void SetupMatchFinder(byte[] input, int startPosition);

        /// <summary>
        /// Setup enumerators for use in <see cref="GetAllMatches(byte[],int)"/>.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="startPosition">The position to start at.</param>
        /// <returns></returns>
        protected abstract MatchFinderEnumerator[] SetupMatchFinderEnumerators(byte[] input, int startPosition);

        /// <inheritdoc cref="Dispose()"/>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FindOptions = null;
                FindLimitations = null;
            }
        }
    }
}
