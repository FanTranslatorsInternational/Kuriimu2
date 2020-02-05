using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Extensions;
using Kompression.IO;
using Kompression.IO.Streams;
using Kompression.PatternMatch.MatchParser.Support;
using Kontract.Kompression;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser
{
    public class OptimalParser2 : IMatchParser
    {
        //private PriceHistoryElement[] _history;

        private readonly IPriceCalculator2 _priceCalculator;
        private readonly IMatchFinder[] _finders;

        public FindOptions FindOptions { get; }

        public OptimalParser2(FindOptions options, IPriceCalculator2 priceCalculator, params IMatchFinder[] finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All match finder have to have the same unit size.");

            FindOptions = options;
            _priceCalculator = priceCalculator;
            _finders = finders;
        }

        public IEnumerable<Match> ParseMatches(Stream input)
        {
            if (FindOptions.PreBufferSize > 0)
                input = new PreBufferStream(input, FindOptions.PreBufferSize);

            if (FindOptions.SearchBackwards)
                input = new ReverseStream(input, input.Length);

            return InternalParseMatches(input.ToArray(), FindOptions.PreBufferSize);
        }

        private IEnumerable<Match> InternalParseMatches(byte[] input, int startPosition)
        {
            foreach (var finder in _finders)
                finder.PreProcess(input, startPosition);

            //_history = Enumerable.Repeat(new PriceHistoryElement { Price = int.MaxValue }, input.Length - startPosition + 1).ToArray();
            //_history[0].Price = 0;

            ForwardPass(input, startPosition);
            //return BackwardPass(input.Length - startPosition).Reverse().ToArray();
            return Array.Empty<Match>();
        }

        //struct Position
        //{
        //    public int position;
        //    public int runLength;
        //    public bool matchRun;

        //    public int historyIndex;

        //    public Position(int position, int runLength, bool matchRun, int historyIndex)
        //    {
        //        this.position = position;
        //        this.runLength = runLength;
        //        this.matchRun = matchRun;
        //        this.historyIndex = historyIndex;
        //    }

        //    public override bool Equals(object obj)
        //    {
        //        if (obj is Position struct1)
        //        {
        //            return position == struct1.position && runLength == struct1.runLength && matchRun == struct1.matchRun;
        //        }

        //        return base.Equals(obj);
        //    }
        //}

        private IList<PositionElement>[] _history2;

        class PositionElement
        {
            public int runLength;
            public bool matchRun;

            public PositionElement parent;
            public int price;

            public PositionElement(int runLength, bool matchRun)
            {
                this.runLength = runLength;
                this.matchRun = matchRun;
            }

            public PositionElement(int runLength, bool matchRun, PositionElement parent, int price) :
                this(runLength, matchRun)
            {
                this.parent = parent;
                this.price = price;
            }
        }

        //private List<Position>[] _historyPositions;
        //private List<PositionElement> _historyElements;

        private void ForwardPass(byte[] input, int startPosition)
        {
            _history2 = new IList<PositionElement>[input.Length - startPosition + (int)FindOptions.UnitSize];
            for (var i = 0; i < _history2.Length; i++)
                _history2[i] = new List<PositionElement>();
            _history2[0].Add(new PositionElement(0, false));

            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)FindOptions.UnitSize;
            for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            {
                foreach (var literalPosition in _history2[dataPosition])
                {
                    var price = _priceCalculator.CalculateLiteralPrice(dataPosition, input[dataPosition],
                        literalPosition.runLength, literalPosition.matchRun);

                    var newRunLength = literalPosition.matchRun ? unitSize : literalPosition.runLength + unitSize;
                    var existingEntry = _history2[dataPosition + unitSize]
                               .FirstOrDefault(x => x.matchRun == false && x.runLength == newRunLength);
                    if (existingEntry == null)
                    {
                        var newEntry = new PositionElement(newRunLength, false,
                            literalPosition, literalPosition.price + price);
                        _history2[dataPosition + unitSize].Add(newEntry);
                    }
                    else
                    {
                        if (existingEntry.price > literalPosition.price + price)
                        {
                            existingEntry.parent = literalPosition;
                            existingEntry.price = literalPosition.price + price;
                        }
                    }
                }

                // Then go through all matches at current position
                for (var finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatch = matches[finderIndex][dataPosition];
                    if (finderMatch == null || !finderMatch.HasMatches)
                        continue;

                    for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= finderMatch.MaxLength; j += unitSize)
                    {
                        foreach (var matchPosition in _history2[dataPosition])
                        {
                            var price = _priceCalculator.CalculateMatchPrice(dataPosition, finderMatch.GetDisplacement(j), j,
                                matchPosition.runLength, matchPosition.matchRun);

                            var newRunLength = matchPosition.matchRun ? matchPosition.runLength + 1 : 1;
                            var existingEntry = _history2[dataPosition + j]
                                .FirstOrDefault(x => x.matchRun == true && x.runLength == newRunLength);
                            if (existingEntry == null)
                            {
                                var newEntry = new PositionElement(newRunLength, true,
                                    matchPosition, matchPosition.price + price);
                                _history2[dataPosition + j].Add(newEntry);
                            }
                            else
                            {
                                if (existingEntry.price > matchPosition.price + price)
                                {
                                    existingEntry.parent = matchPosition;
                                    existingEntry.price = matchPosition.price + price;
                                }
                            }
                        }

                        //var matchPrice = _priceCalculator.CalculateMatchPrice(dataPosition, finderMatches[matchIndex].Displacement, j,);
                        //matchPrice += _history[dataPosition].Price;

                        //if (dataPosition + j < _history.Length &&
                        //    matchPrice < _history[dataPosition + j].Price)
                        //{
                        //    _history[dataPosition + j].IsLiteral = false;
                        //    _history[dataPosition + j].Displacement = finderMatches[matchIndex].Displacement;
                        //    _history[dataPosition + j].Length = j;
                        //    _history[dataPosition + j].Price = matchPrice;
                        //}
                    }
                }
            }

            //var state = new ParserState(_history);

            //var matches = GetAllMatches(input, startPosition);

            //var unitSize = (int)FindOptions.UnitSize;
            //for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            //{
            //    // Calculate literal place at position
            //    var literalPrice = _priceCalculator.CalculateLiteralPrice(state, dataPosition, input[dataPosition]);
            //    literalPrice += _history[dataPosition].Price;
            //    if (dataPosition + unitSize < _history.Length &&
            //        literalPrice < _history[dataPosition + unitSize].Price)
            //    {
            //        _history[dataPosition + unitSize].IsLiteral = true;
            //        _history[dataPosition + unitSize].Price = literalPrice;
            //        _history[dataPosition + unitSize].Length = unitSize;
            //        _history[dataPosition + unitSize].Displacement = 0;
            //    }

            //    // Then go through all longest matches at current position
            //    for (var finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
            //    {
            //        var finderMatches = matches[finderIndex][dataPosition];
            //        if (finderMatches == null || !finderMatches.Any())
            //            continue;

            //        var matchIndex = 0;
            //        for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= finderMatches[finderMatches.Count - 1].Length; j += unitSize)
            //        {
            //            var matchPrice = _priceCalculator.CalculateMatchPrice(state, dataPosition, finderMatches[matchIndex].Displacement, j);
            //            matchPrice += _history[dataPosition].Price;

            //            if (dataPosition + j < _history.Length &&
            //                matchPrice < _history[dataPosition + j].Price)
            //            {
            //                _history[dataPosition + j].IsLiteral = false;
            //                _history[dataPosition + j].Displacement = finderMatches[matchIndex].Displacement;
            //                _history[dataPosition + j].Length = j;
            //                _history[dataPosition + j].Price = matchPrice;
            //            }

            //            if (j + unitSize > finderMatches[matchIndex].Length)
            //                matchIndex++;
            //        }
            //    }
            //}
        }

        //private IEnumerable<Match> BackwardPass(int dataLength)
        //{
        //    var unitSize = (int)_finders[0].FindOptions.UnitSize;

        //    for (var i = dataLength; i > 0;)
        //    {
        //        if (_history[i].IsLiteral)
        //            i -= unitSize;
        //        else
        //        {
        //            yield return new Match(i - _history[i].Length + FindOptions.PreBufferSize, _history[i].Displacement, _history[i].Length);
        //            i -= _history[i].Length + unitSize * FindOptions.SkipUnitsAfterMatch;
        //        }
        //    }
        //}

        class AggregateMatch
        {
            private readonly IList<Match> _matches;
            private readonly (int, int, int)[] _lengthRange;

            public int MaxLength { get; }

            public bool HasMatches { get; }

            public AggregateMatch(IList<Match> matches)
            {
                _matches = matches;
                _lengthRange = new (int, int, int)[matches.Count];

                var prevLength = 1;
                for (var i = 0; i < matches.Count; i++)
                {
                    _lengthRange[i] = (prevLength, matches[i].Length, i);
                    prevLength += matches[i].Length;
                }

                HasMatches = matches.Any();
                if (HasMatches)
                    MaxLength = matches.Last().Length;
            }

            public int GetDisplacement(int length)
            {
                return _matches[_lengthRange.First(x => length >= x.Item1 && length <= x.Item2).Item3].Displacement;
            }
        }

        private IList<IList<AggregateMatch>> GetAllMatches(byte[] input, int startPosition)
        {
            var result = new IList<AggregateMatch>[_finders.Length];

            for (var i = 0; i < _finders.Length; i++)
            {
                result[i] = Enumerable.Range(startPosition, input.Length).AsParallel().AsOrdered()
                    .Select(x => new AggregateMatch(_finders[i].FindMatchesAtPosition(input, x))).ToArray();
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
