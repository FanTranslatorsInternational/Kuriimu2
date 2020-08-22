using System;
using Kompression.PatternMatch.MatchFinders.Support;
using Kontract.Kompression;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchFinders
{
    /// <summary>
    /// Find pattern matches via a history of found values.
    /// </summary>
    public class HistoryMatchFinder : IMatchFinder
    {
        private HistoryMatchState _state;

        /// <inheritdoc />
        public FindLimitations FindLimitations { get; }

        /// <inheritdoc />
        public FindOptions FindOptions { get; }

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchFinder"/>
        /// </summary>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public HistoryMatchFinder(FindLimitations limits, FindOptions options)
        {
            FindLimitations = limits;
            FindOptions = options;
        }

        /// <inheritdoc />
        public void PreProcess(byte[] input, int startPosition)
        {
            _state = new HistoryMatchState(input, startPosition, FindLimitations, FindOptions.UnitSize);
        }

        /// <inheritdoc />
        public AggregateMatch FindMatchesAtPosition(byte[] input, int position)
        {
            if (_state == null)
                throw new InvalidOperationException("Match finder needs to preprocess the input first.");

            return _state.FindMatchesAtPosition(input, position);
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _state?.Dispose();
                _state = null;
            }
        }

        #endregion
    }
}
