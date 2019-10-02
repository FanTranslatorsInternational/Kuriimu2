using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kompression.Models;

namespace Kompression.MatchFinders.Parallel
{
    /// <summary>
    /// Enumerates results from a match finder in a specific interval.
    /// </summary>
    class MatchFinderEnumerator : IEnumerator<Match[]>
    {
        private byte[] _input;
        private int _startPosition;
        private int _interval;
        private Func<byte[], int, IEnumerable<Match>> _getMatchesAtPosition;

        private int _currentPosition;

        /// <inheritdoc cref="Current"/>
        public Match[] Current { get; private set; }

        /// <inheritdoc cref="IEnumerator.Current"/>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Creates a new instance of <see cref="MatchFinderEnumerator"/>.
        /// </summary>
        /// <param name="input">The data to find matches in.</param>
        /// <param name="startPosition">The position into the data to start from.</param>
        /// <param name="interval">The interval in which to increase the position.</param>
        /// <param name="getMatchesAtPosition">The action to find matches.</param>
        public MatchFinderEnumerator(byte[] input, int startPosition, int interval, Func<byte[], int, IEnumerable<Match>> getMatchesAtPosition)
        {
            _input = input;
            _startPosition = startPosition;
            _interval = interval;
            _getMatchesAtPosition = getMatchesAtPosition;

            _currentPosition = _startPosition;
        }

        /// <inheritdoc cref="MoveNext"/>
        public bool MoveNext()
        {
            if (_currentPosition >= _input.Length)
                return false;

            Current = _getMatchesAtPosition(_input, _currentPosition).ToArray();

            _currentPosition += _interval;
            return true;
        }

        /// <inheritdoc cref="Reset"/>
        public void Reset()
        {
            _currentPosition = _startPosition;
        }

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            _getMatchesAtPosition = null;
            _input = null;
        }
    }
}
