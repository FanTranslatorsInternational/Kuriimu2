using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using C5;
using System.Linq;
using Kompression.Models;

namespace Kompression.PatternMatch.LempelZiv.Support
{
    // TODO: Check GetOffset method
    public class SuffixArray : IDisposable
    {
        private const int EndOfData = int.MaxValue;

        private int[] _suffixArray;
        private int[] _inverseSuffixArray;
        private int[] _longestCommonPrefix;

        private HashDictionary<byte, int> _chainHeadsDictionary = new HashDictionary<byte, int>(new ByteComparer());
        private Stack<Chain> _chainStack = new Stack<Chain>();
        private ArrayList<Chain> _subChains = new ArrayList<Chain>();

        private readonly int[] _shortcutArray = new int[256];
        private int[] _suffixHashes;
        private MD5 _hashAlg;

        private readonly bool _buildLongestCommonPrefix;
        private int _nextRank = 1;

        private byte[] _inputData;
        private int _startPosition;

        public bool IsBuilt { get; private set; }

        public int Length => _suffixArray.Length;

        public int this[int index] => _suffixArray[index];

        /// <summary>
        /// Build a suffix array from a byte[].
        /// </summary>
        public SuffixArray() : this(true) { }

        /// 
        /// <summary>
        /// Build a suffix array from a byte[].
        /// </summary>
        /// <param name="buildLongestCommonPrefix">Also calculate longest common prefix information.</param>
        public SuffixArray(bool buildLongestCommonPrefix)
        {
            _buildLongestCommonPrefix = buildLongestCommonPrefix;
        }

        public int[] GetOffsets(int position, int minMatchSize, int minDisplacement, int maxDisplacement, int unitSize)
        {
            var suffixStart = _shortcutArray[_inputData[position]];
            var suffixEnd = _inputData[position] + 1 > 255 ? _suffixArray.Length : _shortcutArray[_inputData[position] + 1];

            var minMatchHash = GetBigEndian(_hashAlg.ComputeHash(_inputData, position, Math.Min(minMatchSize, _inputData.Length - position)));

            var suffixOffsets = _suffixArray.Skip(suffixStart).Take(suffixEnd - suffixStart).
                Where((x, suffixIndex) => _suffixHashes[suffixIndex + suffixStart] == minMatchHash);

            var result = new List<int>();
            foreach (var suffixOffset in suffixOffsets)
            {
                if (suffixOffset <= position - minDisplacement && suffixOffset >= position - maxDisplacement)
                    result.Add(suffixOffset);
            }
            //Where((x, suffixIndex) => suffixIndex + suffixStart <= position - minDisplacement &&
            //                          suffixIndex + suffixStart >= position - maxDisplacement &&
            //                          (position - (suffixIndex + suffixStart)) % (int)unitSize == 0).
            //ToArray();
            return result.ToArray();
        }

        /// <summary>
        /// Build the suffix array.
        /// </summary>
        /// <param name="inputData">Input data for which to build a suffix array.</param>
        /// <param name="startPosition">The position to start creating the suffix array at.</param>
        /// <param name="minMatchSize">The size of the smallest matchable unit; Needed for hash computation.</param>
        public void Build(byte[] inputData, int startPosition, int minMatchSize)
        {
            _inputData = inputData ?? Array.Empty<byte>();
            _startPosition = startPosition;
            _suffixArray = new int[_inputData.Length];
            _inverseSuffixArray = new int[_inputData.Length];

            FormInitialChains();
            BuildSuffixArray();
            if (_buildLongestCommonPrefix)
                BuildLcpArray();

            BuildShortcuts(minMatchSize);

            IsBuilt = true;
        }

        private void FormInitialChains()
        {
            // Link all suffixes that have the same first character
            FindInitialChains();
            SortAndPushSubChains();
        }

        private void FindInitialChains()
        {
            // Scan the string left to right, keeping rightmost occurrences of characters as the chain heads
            for (var i = _startPosition; i < _inputData.Length; i++)
            {
                if (_chainHeadsDictionary.Contains(_inputData[i]))
                {
                    _inverseSuffixArray[i] = _chainHeadsDictionary[_inputData[i]];
                }
                else
                {
                    _inverseSuffixArray[i] = EndOfData;
                }
                _chainHeadsDictionary[_inputData[i]] = i;
            }

            // Prepare chains to be pushed to stack
            foreach (var headIndex in _chainHeadsDictionary.Values)
            {
                var newChain = new Chain(_inputData, headIndex, 1);
                _subChains.Add(newChain);
            }
        }

        private void SortAndPushSubChains()
        {
            _subChains.Sort();
            for (var i = _subChains.Count - 1; i >= 0; i--)
            {
                _chainStack.Push(_subChains[i]);
            }
        }

        private void BuildSuffixArray()
        {
            while (_chainStack.Count > 0)
            {
                // Pop chain
                var chain = _chainStack.Pop();

                if (_inverseSuffixArray[chain.Head] == EndOfData)
                {
                    // Singleton (A chain that contain only 1 suffix)
                    RankSuffix(chain.Head);
                }
                else
                {
                    RefineChainWithInductionSorting(chain);
                }
            }
        }

        private void ExtendChain(Chain chain)
        {
            var sym = _inputData[chain.Head + chain.Length];
            if (_chainHeadsDictionary.Contains(sym))
            {
                // Continuation of an existing chain, this is the leftmost
                // occurence currently known (others may come up later)
                _inverseSuffixArray[_chainHeadsDictionary[sym]] = chain.Head;
                _inverseSuffixArray[chain.Head] = EndOfData;
            }
            else
            {
                // This is the beginning of a new sub chain
                _inverseSuffixArray[chain.Head] = EndOfData;
                var newChain = new Chain(_inputData, chain.Head, chain.Length + 1);
                _subChains.Add(newChain);
            }
            // Save index in case we find a continuation of this chain
            _chainHeadsDictionary[sym] = chain.Head;
        }

        private void RefineChainWithInductionSorting(Chain chain)
        {
            var notedSuffixes = new ArrayList<SuffixRank>();
            _chainHeadsDictionary.Clear();
            _subChains.Clear();

            while (chain.Head != EndOfData)
            {
                int nextIndex = _inverseSuffixArray[chain.Head];
                if (chain.Head + chain.Length > _inputData.Length - 1)
                {
                    // If this substring reaches end of string it cannot be extended.
                    // At this point it's the first in lexicographic order so it's safe
                    // to just go ahead and Rank it.
                    RankSuffix(chain.Head);
                }
                else if (_inverseSuffixArray[chain.Head + chain.Length] < 0)
                {
                    var sr = new SuffixRank
                    {
                        Head = chain.Head,
                        Rank = -_inverseSuffixArray[chain.Head + chain.Length]
                    };
                    notedSuffixes.Add(sr);
                }
                else
                {
                    ExtendChain(chain);
                }

                chain.Head = nextIndex;
            }
            // Keep stack sorted
            SortAndPushSubChains();
            SortAndRankNotedSuffixes(notedSuffixes);
        }

        private void SortAndRankNotedSuffixes(ArrayList<SuffixRank> notedSuffixes)
        {
            notedSuffixes.Sort(new SuffixRankComparer());

            // Rank sorted noted suffixes
            foreach (var notedSuffix in notedSuffixes)
                RankSuffix(notedSuffix.Head);
        }

        private void RankSuffix(int index)
        {
            // We use the ISA to hold both ranks and chain links, so we differentiate by setting
            // the sign.
            _inverseSuffixArray[index] = -_nextRank;
            _suffixArray[_nextRank - 1] = index;
            _nextRank++;
        }

        private void BuildLcpArray()
        {
            _longestCommonPrefix = new int[_suffixArray.Length + 1];
            _longestCommonPrefix[0] = _longestCommonPrefix[_suffixArray.Length] = 0;

            for (var i = 1; i < _suffixArray.Length; i++)
            {
                _longestCommonPrefix[i] = CalcLcp(_suffixArray[i - 1], _suffixArray[i]);
            }
        }

        private int CalcLcp(int i, int j)
        {
            var maxIndex = _inputData.Length - Math.Max(i, j);

            var lcp = 0;
            while (lcp < maxIndex && _inputData[i + lcp] == _inputData[j + lcp])
                lcp++;

            return lcp;
        }

        private void BuildShortcuts(int minMatchSize)
        {
            _suffixHashes = new int[_inputData.Length];
            _hashAlg = MD5.Create();

            var value = -1;
            for (var suffixIndex = 0; suffixIndex < _suffixArray.Length; suffixIndex++)
            {
                var suffixOffset = _suffixArray[suffixIndex];
                if (_inputData[suffixOffset] != value)
                {
                    value = _inputData[suffixOffset];
                    _shortcutArray[value] = suffixIndex;
                }

                var hashValue = _hashAlg.ComputeHash(_inputData, suffixOffset, Math.Min(minMatchSize, _inputData.Length - suffixOffset));
                _suffixHashes[suffixIndex] = GetBigEndian(hashValue);
            }
        }

        private int GetBigEndian(byte[] data)
        {
            return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        }

        public void Dispose()
        {
            _suffixArray = null;
            _inverseSuffixArray = null;
            _longestCommonPrefix = null;

            _chainHeadsDictionary = null;
            _chainStack = null;
            _subChains = null;

            _inputData = null;
        }
    }

    #region Comparer

    [Serializable]
    internal class ByteComparer : System.Collections.Generic.EqualityComparer<byte>
    {
        public override bool Equals(byte x, byte y)
        {
            return x.Equals(y);
        }

        public override int GetHashCode(byte obj)
        {
            return obj.GetHashCode();
        }
    }

    [Serializable]
    internal class SuffixRankComparer : IComparer<SuffixRank>
    {
        public int Compare(SuffixRank x, SuffixRank y)
        {
            return x.Rank.CompareTo(y.Rank);
        }
    }

    #endregion
}
