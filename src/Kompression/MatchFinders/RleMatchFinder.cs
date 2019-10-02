using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.MatchFinders.Parallel;
using Kompression.Models;

namespace Kompression.MatchFinders
{
    /// <summary>
    /// Find sequences of the same value.
    /// </summary>
    public class RleMatchFinder : IMatchFinder
    {
        /// <inheritdoc cref="FindLimitations"/>
        public FindLimitations FindLimitations { get; private set; }

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="RleMatchFinder"/>.
        /// </summary>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public RleMatchFinder(FindLimitations limits, FindOptions options)
        {
            FindLimitations = limits;
            FindOptions = options;
        }

        /// <inheritdoc cref="FindMatchesAtPosition"/>
        public IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            if (input.Length - position < FindLimitations.MinLength)
                yield break;

            var maxLength = FindLimitations.MaxLength <= 0 ? input.Length : FindLimitations.MaxLength;

            var cappedLength = Math.Min(maxLength, input.Length - position);
            for (var repetitions = 0; repetitions < cappedLength; repetitions += (int)FindOptions.UnitSize)
            {
                switch (FindOptions.UnitSize)
                {
                    case UnitSize.Byte:
                        if (input[position + 1 + repetitions] != input[position])
                        {
                            if (repetitions > 0)
                                yield return new Match(position, 0, repetitions);
                            yield break;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"UnitSize '{FindOptions.UnitSize}' is not supported.");
                }
            }

            yield return new Match(position, 0, cappedLength - cappedLength % (int)FindOptions.UnitSize);
        }

        /// <inheritdoc cref="GetAllMatches"/>
        public IEnumerable<Match[]> GetAllMatches(byte[] input, int position)
        {
            var taskCount = FindOptions.TaskCount;
            var unitSize = (int)FindOptions.UnitSize;

            var tasks = new Task<bool>[taskCount];
            var enumerators = new MatchFinderEnumerator[taskCount];

            // Setup enumerators
            for (var i = 0; i < taskCount; i++)
                enumerators[i] = new MatchFinderEnumerator(input, position, taskCount * unitSize, FindMatchesAtPosition);

            // Execute all tasks until end of file
            var continueExecution = true;
            while (continueExecution)
            {
                for (int i = 0; i < taskCount; i++)
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

        #region Dispose

        public void Dispose()
        {
            FindLimitations = null;
            FindOptions = null;
        }

        #endregion
    }
}
