using System;
using System.Collections.Generic;
using Kompression.PatternMatch.MatchFinders;
using Kompression.PatternMatch.MatchParser;
using Kontract;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;

namespace Kompression.Configuration
{
    /// <summary>
    /// Contains information on configuring match finding and parsing.
    /// </summary>
    class MatchOptions : IMatchOptions, IMatchLimitations, IMatchAdditionalFinders
    {
        private static readonly Func<FindLimitations, FindOptions, IMatchFinder> DefaultMatchFinder =
            (limits, options) => new HistoryMatchFinder(limits, options);

        private bool _searchBackwards;
        private int _preBufferSize;
        private int _skipAfterMatch;
        private UnitSize _unitSize = UnitSize.Byte;
        private int _taskCount = Environment.ProcessorCount;

        private readonly IList<Func<FindLimitations, FindOptions, IMatchFinder>> _matchFinderFactories =
            new List<Func<FindLimitations, FindOptions, IMatchFinder>>
            {
                DefaultMatchFinder
            };

        private readonly IList<Func<FindLimitations>> _limitFactories =
            new List<Func<FindLimitations>>();

        private Func<IPriceCalculator> _priceCalculatorFactory;

        private Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> _matchParserFactory =
            (options, calculator, finders) => new OptimalParser(options, calculator, finders);

        /// <inheritdoc cref="WithDefaultMatchFinder"/>
        public IMatchLimitations WithDefaultMatchFinder => FindMatchesWith(DefaultMatchFinder);

        /// <inheritdoc cref="AndWithDefaultMatchFinder"/>
        public IMatchLimitations AndWithDefaultMatchFinder => AndWith(DefaultMatchFinder);

        /// <inheritdoc cref="CalculatePricesWith"/>
        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            ContractAssertions.IsNotNull(priceCalculatorFactory, nameof(priceCalculatorFactory));

            _priceCalculatorFactory = priceCalculatorFactory;

            return this;
        }

        /// <inheritdoc cref="FindMatchesWith"/>
        public IMatchLimitations FindMatchesWith(Func<FindLimitations, FindOptions, IMatchFinder> matchFinderFactory)
        {
            ContractAssertions.IsNotNull(matchFinderFactory, nameof(matchFinderFactory));

            _matchFinderFactories.Clear();
            _limitFactories.Clear();

            _matchFinderFactories.Add(matchFinderFactory);

            return this;
        }

        public IMatchLimitations AndWith(Func<FindLimitations, FindOptions, IMatchFinder> matchFinderFactory)
        {
            ContractAssertions.IsNotNull(matchFinderFactory, nameof(matchFinderFactory));

            _matchFinderFactories.Add(matchFinderFactory);

            return this;
        }

        /// <inheritdoc cref="WithinLimitations"/>
        public IMatchAdditionalFinders WithinLimitations(Func<FindLimitations> limitFactory)
        {
            ContractAssertions.IsNotNull(limitFactory, nameof(limitFactory));

            _limitFactories.Add(limitFactory);

            return this;
        }

        /// <inheritdoc cref="ParseMatchesWith"/>
        public IMatchOptions ParseMatchesWith(Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> matchParserFactory)
        {
            ContractAssertions.IsNotNull(matchParserFactory, nameof(matchParserFactory));

            _matchParserFactory = matchParserFactory;

            return this;
        }

        /// <inheritdoc cref="FindInBackwardOrder"/>
        public IMatchOptions FindInBackwardOrder()
        {
            _searchBackwards = true;
            return this;
        }

        /// <inheritdoc cref="WithPreBufferSize"/>
        public IMatchOptions WithPreBufferSize(int size)
        {
            _preBufferSize = size;
            return this;
        }

        /// <inheritdoc cref="SkipUnitsAfterMatch"/>
        public IMatchOptions SkipUnitsAfterMatch(int skip)
        {
            _skipAfterMatch = skip;
            return this;
        }

        /// <inheritdoc cref="ProcessWithTasks"/>
        public IMatchOptions ProcessWithTasks(int count)
        {
            _taskCount = count;
            return this;
        }

        /// <inheritdoc cref="WithUnitSize"/>
        public IMatchOptions WithUnitSize(UnitSize unitSize)
        {
            _unitSize = unitSize;
            return this;
        }

        internal IMatchParser BuildMatchParser()
        {
            var options = BuildOptions();
            var matchFinders = BuildMatchFinders(options);
            var priceCalculator = _priceCalculatorFactory?.Invoke();

            return _matchParserFactory(options, priceCalculator, matchFinders);
        }

        private FindOptions BuildOptions()
        {
            return new FindOptions(_searchBackwards, _preBufferSize, _skipAfterMatch, _unitSize, _taskCount);
        }

        private IMatchFinder[] BuildMatchFinders(FindOptions options)
        {
            if (_matchFinderFactories.Count != _limitFactories.Count)
                throw new InvalidOperationException("Not all match finders have limitations to search patterns in.");

            var matchFinders = new IMatchFinder[_limitFactories.Count];

            for (var i = 0; i < _limitFactories.Count; i++)
            {
                var limits = _limitFactories[i]();
                var matchFinder = _matchFinderFactories[i](limits, options);

                matchFinders[i] = matchFinder;
            }

            return matchFinders;
        }
    }
}
