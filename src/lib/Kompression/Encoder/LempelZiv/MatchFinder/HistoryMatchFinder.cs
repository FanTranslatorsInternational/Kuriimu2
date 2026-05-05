using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Encoder.LempelZiv.MatchFinder;
using Kompression.Encoder.LempelZiv.MatchFinder.HistoryMatch;

namespace Kompression.Encoder.LempelZiv.MatchFinder
{
    /// <summary>
    /// Find pattern matches via a history of found values.
    /// </summary>
    public class HistoryMatchFinder(LempelZivMatchFinderOptions options) : ILempelZivMatchFinder
    {
        private HistoryMatchState? _state;

        /// <inheritdoc />
        public LempelZivMatchFinderOptions Options { get; } = options;

        /// <inheritdoc />
        public void PreProcess(byte[] input)
        {
            _state = new HistoryMatchState(input, Options);
        }

        /// <inheritdoc />
        public LempelZivAggregateMatch? FindMatchesAtPosition(byte[] input, int position)
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
