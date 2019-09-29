using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Interfaces;
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
        public IEnumerable<Match> GetAllMatches(byte[] input, int position)
        {
            var tasks = new Task<IList<Match>>[FindOptions.TaskCount];

            for (int i = 0; i < tasks.Length; i += (int)FindOptions.UnitSize)
            {
                var getTaskPosition = position + i;
                tasks[i] = new Task<IList<Match>>(() => GetMatchFromTask(input, getTaskPosition, tasks.Length).ToList());
                tasks[i].Start();
            }

            Task.WaitAll(tasks);

            return tasks.SelectMany(x => x.Result).OrderBy(x => x.Position);
        }

        private IEnumerable<Match> GetMatchFromTask(byte[] input, int startPosition, int interval)
        {
            for (var i = startPosition; i < input.Length; i += interval)
            {
                var match = FindMatchesAtPosition(input, i).FirstOrDefault();
                if (match != null)
                    yield return match;
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
