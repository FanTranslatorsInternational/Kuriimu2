using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.PatternMatch;

namespace Kompression.Implementations.MatchFinders
{
    class BackwardLz77MatchFinder : IMatchFinder
    {
        private int windowPos;
        private int windowLen;
        private int[] _offsetTable;
        private int[] _reversedOffsetTable;
        private int[] _byteTable = Enumerable.Repeat(-1, 256).ToArray();
        // Holds last offset of each byte value
        private int[] _endTable = Enumerable.Repeat(-1, 256).ToArray();

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int MinDisplacement { get; }
        public int MaxDisplacement { get; }
        public DataType DataType { get; }
        public bool UseLookAhead { get; }

        public BackwardLz77MatchFinder(int minMatchSize, int maxMatchSize, int minDisplacement, int maxDisplacement,
            bool lookAhead = true, DataType dataType = DataType.Byte)
        {
            if (minMatchSize % (int)dataType != 0 || maxMatchSize % (int)dataType != 0 ||
                minDisplacement % (int)dataType != 0 || maxDisplacement % (int)dataType != 0)
                throw new InvalidOperationException("All values must be dividable by data type.");

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            MinDisplacement = minDisplacement;
            MaxDisplacement = maxDisplacement;
            DataType = dataType;
            UseLookAhead = lookAhead;

            _offsetTable = new int[MaxDisplacement];
            _reversedOffsetTable = new int[MaxDisplacement];
        }

        private int _previousPosition = -1;
        public IEnumerable<Match> FindMatches(byte[] input, int position)
        {
            if (_previousPosition == -1)
            {
                for (var i = 0; i < position; i += (int)DataType)
                    SlideByte(input, i);
            }
            else
            {
                for (var i = 0; i < position - _previousPosition; i += (int)DataType)
                    SlideByte(input, _previousPosition + i);
            }
            _previousPosition = position;

            var displacement = 0;
            var maxSize = Math.Min(input.Length - position, MaxMatchSize);

            if (maxSize < MinMatchSize)
                yield break;

            var size = MinMatchSize - 1;
            for (var nOffset = _endTable[input[position]]; nOffset != -1; nOffset = _reversedOffsetTable[nOffset])
            {
                var search = position + nOffset - windowPos;
                if (nOffset >= windowPos)
                {
                    search -= windowLen;
                }

                if (position - search < MinMatchSize)
                {
                    continue;
                }

                var isMatch = true;
                for (var i = 1; i < MinMatchSize; i++)
                    if (input[search + i] != input[position + i])
                    {
                        isMatch = false;
                        break;
                    }
                if (!isMatch)
                    continue;

                var nMaxSize = maxSize;
                if (!UseLookAhead)
                    nMaxSize = Math.Min(maxSize, position - search);
                var nCurrentSize = MinMatchSize;
                while (nCurrentSize < nMaxSize)
                {
                    if (input[search + nCurrentSize] != input[position + nCurrentSize])
                        break;
                    if (DataType == DataType.Short)
                        if (input[search + nCurrentSize + 1] != input[position + nCurrentSize + 1])
                            break;
                    nCurrentSize += (int)DataType;
                }

                if (nCurrentSize > size)
                {
                    size = nCurrentSize;
                    displacement = position - search;
                    if (size == maxSize)
                        break;
                }
            }

            if (size < MinMatchSize || displacement < MinDisplacement)
                yield break;

            yield return new Match(position, displacement, size);
        }

        //public int Search(int src, out int offset, int maxSize)
        //{
        //    offset = 0;

        //    if (maxSize < 3)
        //    {
        //        return 0;
        //    }

        //    int size = 2;

        //    for (var nOffset = _endTable[input[src - 1]]; nOffset != -1; nOffset = _reversedOffsetTable[nOffset])
        //    {
        //        var search = src - nOffset + windowPos;
        //        if (nOffset >= windowPos)
        //        {
        //            search += windowLen;
        //        }

        //        if (search - src < MinMatchSize)
        //        {
        //            continue;
        //        }

        //        if (input[search - 2] != input[src - 2] || input[search - 3] != input[src - 3])
        //        {
        //            continue;
        //        }

        //        int nMaxSize = Math.Min(maxSize, search - src);
        //        int nCurrentSize = 3;
        //        while (nCurrentSize < nMaxSize && input[search - nCurrentSize - 1] == input[src - nCurrentSize - 1])
        //        {
        //            nCurrentSize++;
        //        }

        //        if (nCurrentSize > size)
        //        {
        //            size = nCurrentSize;
        //            offset = search - src;
        //            if (size == maxSize)
        //            {
        //                break;
        //            }
        //        }
        //    }

        //    if (size < 3)
        //    {
        //        return 0;
        //    }

        //    return size;
        //}

        public void SlideByte(byte[] input, int position)
        {
            byte uInData = input[position];
            int uInsertOffset;

            if (windowLen == MaxDisplacement)
            {
                var uOutData = input[position - MaxDisplacement];

                if ((_byteTable[uOutData] = _offsetTable[_byteTable[uOutData]]) == -1)
                {
                    _endTable[uOutData] = -1;
                }
                else
                {
                    _reversedOffsetTable[_byteTable[uOutData]] = -1;
                }

                uInsertOffset = windowPos;
            }
            else
            {
                uInsertOffset = windowLen;
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

            if (windowLen == MaxDisplacement)
            {
                windowPos+=(int)DataType;
                windowPos %= MaxDisplacement;
            }
            else
            {
                windowLen += (int)DataType;
            }
        }

        public void Dispose()
        {
            _endTable = null;
            _reversedOffsetTable = null;
            _byteTable = null;
            _offsetTable = null;
        }
    }
}
