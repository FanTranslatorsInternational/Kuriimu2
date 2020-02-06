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
    public class OptimalParser3 : IMatchParser
    {
        private PositionElement[] _history;

        private readonly IPriceCalculator2 _priceCalculator;
        private readonly IMatchFinder[] _finders;

        public FindOptions FindOptions { get; }

        public OptimalParser3(FindOptions options, IPriceCalculator2 priceCalculator, params IMatchFinder[] finders)
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

            _history = new PositionElement[input.Length - startPosition + 1];
            for (var i = 0; i < _history.Length; i++)
                _history[i] = new PositionElement(0, 0, false, null, int.MaxValue);
            _history[0].price = 0;

            ForwardPass(input, startPosition);
            return BackwardPass().Reverse().ToArray();
            //return Array.Empty<Match>();
        }

        class PositionElement
        {
            public int position;
            public int runLength;
            public bool matchRun;

            public PositionElement parent;
            public int price;

            public Match? match;

            public PositionElement(int position, int runLength, bool matchRun)
            {
                this.position = position;
                this.runLength = runLength;
                this.matchRun = matchRun;
            }

            public PositionElement(int position, int runLength, bool matchRun, PositionElement parent, int price) :
                this(position, runLength, matchRun)
            {
                this.parent = parent;
                this.price = price;
            }
        }

        private void ForwardPass(byte[] input, int startPosition)
        {
            //var state = new ParserState(_history);

            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)FindOptions.UnitSize;
            for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            {
                // Calculate literal place at position
                var element = _history[dataPosition];
                var literalPrice = _priceCalculator.CalculateLiteralPrice(dataPosition, input[dataPosition], element.runLength, element.matchRun);
                literalPrice += element.price;
                if (dataPosition + unitSize < _history.Length)
                {
                    if (literalPrice <= _history[dataPosition + unitSize].price)
                    {
                        var nextElement = _history[dataPosition + unitSize];

                        nextElement.position = dataPosition + unitSize;
                        nextElement.parent = element;
                        nextElement.price = literalPrice;
                        nextElement.runLength = (element.matchRun ? 0 : element.runLength) + unitSize;
                        nextElement.matchRun = false;
                        nextElement.match = null;
                    }
                }

                // Then go through all longest matches at current position
                for (var finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatch = matches[finderIndex][dataPosition];
                    if (finderMatch == null || !finderMatch.HasMatches)
                        continue;

                    for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= finderMatch.MaxLength; j += unitSize)
                    {
                        var displacement = finderMatch.GetDisplacement(j);
                        var matchPrice = _priceCalculator.CalculateMatchPrice(dataPosition + j, displacement,
                            j, element.runLength, element.matchRun);
                        matchPrice += element.price;

                        if (dataPosition + j < _history.Length &&
                            matchPrice < _history[dataPosition + j].price)
                        {
                            var nextElement = _history[dataPosition + j];

                            nextElement.position = dataPosition + j;
                            nextElement.parent = element;
                            nextElement.price = matchPrice;
                            nextElement.runLength = (element.matchRun ? element.runLength : 0) + 1;
                            nextElement.matchRun = true;
                            nextElement.match = new Match(dataPosition + FindOptions.PreBufferSize, displacement, j);
                        }
                    }
                }
            }
        }

        private IEnumerable<Match> BackwardPass()
        {
            var element = _history.Last();
            while (element != null)
            {
                if (element.match.HasValue)
                    yield return element.match.Value;

                element = element.parent;
            }
        }

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
                    prevLength = matches[i].Length + 1;
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
