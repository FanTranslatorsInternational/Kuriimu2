using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.Configuration;
using Kompression.Models;

namespace Kompression.MatchFinders.Support
{
    /// <summary>
    /// The state machine for <see cref="HistoryMatchFinder"/>.
    /// </summary>
    class HistoryMatchState : IDisposable
    {
        private int _previousPosition = -1;

        private int _windowPos;
        private int _windowLen;

        private int[] _offsetTable;
        private int[] _reversedOffsetTable;

        private int[] _byteTable = Enumerable.Repeat(-1, 256).ToArray();
        private int[] _endTable = Enumerable.Repeat(-1, 256).ToArray();

        private FindLimitations _limits;
        private FindOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchState"/>.
        /// </summary>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public HistoryMatchState(FindLimitations limits, FindOptions options)
        {
            _limits = limits;
            _options = options;
        }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        public IEnumerable<Match> FindMatchAtPosition(byte[] input, int position)
        {
            var maxLength = _limits.MaxLength <= 0 ? input.Length : _limits.MaxLength;
            var maxDisplacement = _limits.MaxDisplacement <= 0 ? input.Length : _limits.MaxDisplacement;

            _offsetTable = new int[maxDisplacement];
            _reversedOffsetTable = new int[maxDisplacement];

            if (_previousPosition == -1)
            {
                for (var i = 0; i < position; i += (int)_options.UnitSize)
                    SlideByte(input, i);
            }
            else
            {
                for (var i = 0; i < position - _previousPosition; i += (int)_options.UnitSize)
                    SlideByte(input, _previousPosition + i);
            }
            _previousPosition = position;

            var cappedLength = Math.Min(input.Length - position, maxLength);

            if (cappedLength < _limits.MinLength)
                yield break;

            var size = _limits.MinLength - 1;
            for (var nOffset = _endTable[input[position]]; nOffset != -1; nOffset = _reversedOffsetTable[nOffset])
            {
                var search = position + nOffset - _windowPos;
                if (nOffset >= _windowPos)
                {
                    search -= _windowLen;
                }

                if (position - search < Math.Min(_limits.MinLength, _limits.MinDisplacement))
                    continue;

                var isMatch = true;
                for (var i = 1; i < _limits.MinLength; i++)
                    if (input[search + i] != input[position + i])
                    {
                        isMatch = false;
                        break;
                    }
                if (!isMatch)
                    continue;

                var nMaxSize = cappedLength;
                var nCurrentSize = _limits.MinLength;
                switch (_options.UnitSize)
                {
                    case UnitSize.Byte:
                        while (nCurrentSize < nMaxSize)
                        {
                            if (input[search + nCurrentSize] != input[position + nCurrentSize])
                                break;
                            nCurrentSize++;
                        }
                        break;
                    case UnitSize.Short:
                        while (nCurrentSize < nMaxSize)
                        {
                            if (input[search + nCurrentSize] != input[position + nCurrentSize] ||
                                input[search + nCurrentSize + 1] != input[position + nCurrentSize + 1])
                                break;
                            nCurrentSize += 2;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"UnitSize '{_options.UnitSize}' is not supported.");
                }

                if (nCurrentSize > size)
                {
                    // Return all matches up to the longest
                    yield return new Match(position,position-search,nCurrentSize);

                    size = nCurrentSize;
                    if (size == cappedLength)
                        yield break;
                }
            }

            //if (size < _limits.MinLength || foundDisplacement < _limits.MinDisplacement)
            //    yield break;

            //yield return new Match(position, foundDisplacement, size);
        }

        private void SlideByte(byte[] input, int position)
        {
            var maxDisplacement = _limits.MaxDisplacement <= 0 ? input.Length : _limits.MaxDisplacement;

            byte uInData = input[position];
            int uInsertOffset;

            if (_windowLen == maxDisplacement)
            {
                var uOutData = input[position - maxDisplacement];

                if ((_byteTable[uOutData] = _offsetTable[_byteTable[uOutData]]) == -1)
                {
                    _endTable[uOutData] = -1;
                }
                else
                {
                    _reversedOffsetTable[_byteTable[uOutData]] = -1;
                }

                uInsertOffset = _windowPos;
            }
            else
            {
                uInsertOffset = _windowLen;
            }

            var nOffset = _endTable[uInData];
            if (nOffset == -1)
            {
                _byteTable[uInData] = uInsertOffset;
            }
            else
            {
                _offsetTable[nOffset] = uInsertOffset;
            }

            _endTable[uInData] = uInsertOffset;
            _offsetTable[uInsertOffset] = -1;
            _reversedOffsetTable[uInsertOffset] = nOffset;

            if (_windowLen == maxDisplacement)
            {
                _windowPos += (int)_options.UnitSize;
                _windowPos %= maxDisplacement;
            }
            else
            {
                _windowLen += (int)_options.UnitSize;
            }
        }

        #region Dispose

        public void Dispose()
        {
            _offsetTable = null;
            _reversedOffsetTable = null;

            _byteTable = null;
            _endTable = null;

            _options = null;
            _limits = null;
        }

        #endregion
    }
}
