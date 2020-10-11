using System;
using System.Collections.Generic;
using Kompression.Configuration.InputManipulation;
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
    class MatchOptions : IMatchLimitations, IMatchAdditionalFinders
    {
        private static readonly Func<FindLimitations, FindOptions, IMatchFinder> DefaultMatchFinder =
            (limits, options) => new HistoryMatchFinder(limits, options);

        private static readonly Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> DefaultMatchParser =
            (options, priceCalculator, finders) => new OptimalParser(options, priceCalculator, finders);

        private readonly IList<Func<FindLimitations, FindOptions, IMatchFinder>> _matchFinderFactories =
            new List<Func<FindLimitations, FindOptions, IMatchFinder>>
            {
                DefaultMatchFinder
            };

        private readonly IList<Func<FindLimitations>> _limitFactories =
            new List<Func<FindLimitations>>();

        private int _skipAfterMatch;
        private UnitSize _unitSize = UnitSize.Byte;
        private int _taskCount = Environment.ProcessorCount;

        private Func<IPriceCalculator> _priceCalculatorFactory;
        private Action<IInputConfiguration> _inputConfigurationFactory;

        private Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> _matchParserFactory = DefaultMatchParser;

        /// <inheritdoc cref="FindMatchesWithDefault"/>
        public IMatchLimitations FindMatchesWithDefault() => FindMatchesWith(DefaultMatchFinder);

        /// <inheritdoc cref="AndWithDefault"/>
        public IMatchLimitations AndWithDefault() => AndWith(DefaultMatchFinder);

        /// <inheritdoc cref="FindMatchesWith"/>
        public IMatchLimitations FindMatchesWith(Func<FindLimitations, FindOptions, IMatchFinder> matchFinderFactory)
        {
            ContractAssertions.IsNotNull(matchFinderFactory, nameof(matchFinderFactory));

            _matchFinderFactories.Clear();
            _limitFactories.Clear();

            _matchFinderFactories.Add(matchFinderFactory);

            return this;
        }

        /// <inheritdoc cref="AndWith"/>
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

        /// <inheritdoc cref="CalculatePricesWith"/>
        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            ContractAssertions.IsNotNull(priceCalculatorFactory, nameof(priceCalculatorFactory));

            _priceCalculatorFactory = priceCalculatorFactory;

            return this;
        }

        public IMatchOptions AdjustInput(Action<IInputConfiguration> inputConfigurationFactory)
        {
            ContractAssertions.IsNotNull(inputConfigurationFactory, nameof(inputConfigurationFactory));

            _inputConfigurationFactory = inputConfigurationFactory;

            return this;
        }

        /// <inheritdoc cref="ParseMatchesWith"/>
        public IMatchOptions ParseMatchesWith(Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> matchParserFactory)
        {
            ContractAssertions.IsNotNull(matchParserFactory, nameof(matchParserFactory));

            _matchParserFactory = matchParserFactory;

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
            var inputManipulator = BuildInputManipulator();
            return new FindOptions(inputManipulator, _skipAfterMatch, _unitSize, _taskCount);
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

        private IInputManipulator BuildInputManipulator()
        {
            var inputConfig = new InputConfiguration();
            _inputConfigurationFactory?.Invoke(inputConfig);

            return inputConfig.Build();
        }
    }
}
