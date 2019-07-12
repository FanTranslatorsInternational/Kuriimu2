//using System;
//using System.IO;
//using Kompression.LempelZiv.Matcher;
//using Kompression.LempelZiv.Models;

//namespace Kompression.LempelZiv.MatchFinder
//{
//    public class SuffixArrayParser : ILzParser
//    {
//        private readonly MatchType _matchType;

//        public int WindowSize { get; }
//        public int MinMatchSize { get; }
//        public int MaxMatchSize { get; }

//        public SuffixArrayParser(MatchType matchType, int windowSize, int minMatchSize, int maxMatchSize)
//        {
//            _matchType = matchType;

//            WindowSize = windowSize;
//            MinMatchSize = minMatchSize;
//            MaxMatchSize = maxMatchSize;
//        }

//        public LzMatch[] FindMatches(Stream input)
//        {
//            var inputArray = ToArray(input);

//            switch (_matchType)
//            {
//                case MatchType.Greedy:
//                    return FindGreedyMatches(inputArray, (int)input.Position);
//                case MatchType.OptimalParser:
//                    return FindOptimalMatches(inputArray, (int)input.Position);
//                default:
//                    throw new InvalidOperationException($"Match type {_matchType} not supported.");
//            }
//        }

//        private LzMatch[] FindGreedyMatches(byte[] input, int position)
//        {
//            var suffixArray = SuffixArray.Create(input);
//        }

//        private LzMatch[] FindOptimalMatches(byte[] input, int position)
//        {
//            var suffixArray = SuffixArray.Create(input);
//        }

//        private byte[] ToArray(Stream input)
//        {
//            var bkPos = input.Position;
//            var inputArray = new byte[input.Length];
//            input.Read(inputArray, 0, inputArray.Length);
//            input.Position = bkPos;

//            return inputArray;
//        }

//        #region Dispose

//        public void Dispose()
//        {
//            Dispose(true);
//        }

//        private void Dispose(bool dispose)
//        {
//            // Nothing to dispose
//        }

//        #endregion
//    }
//}
