using System.Collections.Generic;
using Kompression.Configuration;
using Kompression.Models;
using Kompression.PatternMatch.MatchFinders.Parallel;
using Kompression.PatternMatch.MatchFinders.Support;

namespace Kompression.PatternMatch.MatchFinders
{
    /// <summary>
    /// Find pattern matches via a history of found values.
    /// </summary>
    public class HistoryMatchFinder : BaseMatchFinder
    {
        private HistoryMatchState[] _states;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchFinder"/>
        /// </summary>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public HistoryMatchFinder(FindLimitations limits, FindOptions options) :
            base(limits, options)
        {
        }

        /// <inheritdoc cref="FindMatchesAtPosition(byte[],int)"/>
        public override IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            if (_states == null)
            {
                _states = new HistoryMatchState[1];
                _states[0] = new HistoryMatchState(input, FindLimitations, FindOptions);
            }

            return _states[0].FindMatchesAtPosition(input, position);
        }

        protected override void SetupMatchFinder(byte[] input, int startPosition)
        {
            _states = new HistoryMatchState[FindOptions.TaskCount];
            for (var i = 0; i < _states.Length; i++)
                _states[i] = new HistoryMatchState(input, FindLimitations, FindOptions);
        }

        protected override MatchFinderEnumerator[] SetupMatchFinderEnumerators(byte[] input, int startPosition)
        {
            var taskCount = FindOptions.TaskCount;
            var unitSize = (int)FindOptions.UnitSize;

            var enumerators = new MatchFinderEnumerator[taskCount];
            for (var i = 0; i < taskCount; i++)
                enumerators[i] = new MatchFinderEnumerator(input, startPosition + unitSize * i, taskCount * unitSize, _states[i].FindMatchesAtPosition);

            return enumerators;
        }

        /// <inheritdoc cref="Reset"/>
        public override void Reset()
        {
            if (_states != null)
                foreach (var state in _states)
                    state.Dispose();
            _states = null;
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_states != null)
                    foreach (var state in _states)
                        state.Dispose();
                _states = null;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
