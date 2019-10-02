using System;
using System.Collections.Generic;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.MatchFinders.Support;
using Kompression.Models;

namespace Kompression.MatchFinders
{
    /// <summary>
    /// Find pattern matches via a history of found values.
    /// </summary>
    public class HistoryMatchFinder : IMatchFinder
    {
        private HistoryMatchState _state;

        /// <inheritdoc cref="FindLimitations"/>
        public FindLimitations FindLimitations { get; private set; }

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

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

        /// <inheritdoc cref="FindMatchesAtPosition"/>
        public IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            if (_state == null)
                _state = new HistoryMatchState(FindLimitations, FindOptions);

            return _state.FindMatchAtPosition(input, position);
        }

        /// <inheritdoc cref="GetAllMatches"/>
        public IEnumerable<Match[]> GetAllMatches(byte[] input, int position)
        {
            throw new NotSupportedException();
        }

        #region Dispose

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            _state?.Dispose();

            _state = null;
            FindLimitations = null;
            FindOptions = null;
        }

        #endregion
    }
}
