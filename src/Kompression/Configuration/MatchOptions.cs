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
    class MatchOptions : IAdditionalMatchFinder, IMatchLimitations
    {
        #region Static Fields

        private static readonly Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> DefaultMatchParser =
            (options, priceCalculator, finders) => new OptimalParser(options, priceCalculator, finders);

        #endregion

        private Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> _matchParserFactory = DefaultMatchParser;

        private Func<IPriceCalculator> _priceCalculatorFactory;
        private Action<IInputConfiguration> _inputConfigurationFactory;

        private IList<Func<FindOptions, FindLimitations, IMatchFinder>> _matchFinderFactories;
        private IList<Func<FindLimitations>> _limitFactories;

        private int _skipAfterMatch;
        private UnitSize _unitSize = UnitSize.Byte;
        private int _taskCount = Environment.ProcessorCount;

        #region ParseWith method declarations

        public IInternalMatchOptions ParseOptimal()
        {
            return ParseMatchesWith(DefaultMatchParser);
        }

        #endregion

        #region FindWith method declarations

        /// <inheritdoc cref="FindWith"/>
        public IMatchLimitations FindWith(Func<FindOptions, FindLimitations, IMatchFinder> matchFinderFactory)
        {
            ContractAssertions.IsNotNull(matchFinderFactory, nameof(matchFinderFactory));

            _limitFactories?.Clear();
            _matchFinderFactories = new List<Func<FindOptions, FindLimitations, IMatchFinder>>
            {
                matchFinderFactory
            };

            return this;
        }

        /// <inheritdoc cref="FindMatches"/>
        public IMatchLimitations FindMatches()
        {
            return FindWith((options, limitations) => new HistoryMatchFinder(limitations, options));
        }

        /// <inheritdoc cref="FindRunLength"/>
        public IMatchLimitations FindRunLength()
        {
            return FindWith((options, limitations) => new RleMatchFinder(limitations, options));
        }

        /// <inheritdoc cref="FindConstantRunLength"/>
        public IMatchLimitations FindConstantRunLength(int constant)
        {
            return FindWith((options, limitations) => new StaticValueRleMatchFinder(constant, limitations, options));
        }

        /// <inheritdoc cref="AndFindWith"/>
        public IMatchLimitations AndFindWith(Func<FindOptions, FindLimitations, IMatchFinder> matchFinderFactory)
        {
            ContractAssertions.IsNotNull(matchFinderFactory, nameof(matchFinderFactory));

            _matchFinderFactories.Add(matchFinderFactory);

            return this;
        }

        /// <inheritdoc cref="AndFindMatches"/>
        public IMatchLimitations AndFindMatches()
        {
            return AndFindWith((options, limitations) => new HistoryMatchFinder(limitations, options));
        }

        /// <inheritdoc cref="AndFindRunLength"/>
        public IMatchLimitations AndFindRunLength()
        {
            return AndFindWith((options, limitations) => new RleMatchFinder(limitations, options));
        }

        /// <inheritdoc cref="AndFindConstantRunLength"/>
        public IMatchLimitations AndFindConstantRunLength(int constant)
        {
            return AndFindWith((options, limitations) => new StaticValueRleMatchFinder(constant, limitations, options));
        }

        #endregion

        #region Limitations

        /// <inheritdoc cref="WithinLimitations(int,int)"/>
        public IAdditionalMatchFinder WithinLimitations(int minLength, int maxLength)
        {
	        return WithinLimitations(() => new FindLimitations(minLength, maxLength));
        }

        /// <inheritdoc cref="WithinLimitations(int,int,int,int)"/>
        public IAdditionalMatchFinder WithinLimitations(int minLength, int maxLength, int minDisplacement, int maxDisplacement)
        {
	        return WithinLimitations(() => new FindLimitations(minLength, maxLength, minDisplacement, maxDisplacement));
        }

        #endregion

        #region General

        /// <inheritdoc cref="ParseMatchesWith"/>
        public IInternalMatchOptions ParseMatchesWith(Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> matchParserFactory)
        {
	        ContractAssertions.IsNotNull(matchParserFactory, nameof(matchParserFactory));

	        _matchParserFactory = matchParserFactory;

	        return this;
        }

        /// <inheritdoc cref="CalculatePricesWith"/>
        public IInternalMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
	        ContractAssertions.IsNotNull(priceCalculatorFactory, nameof(priceCalculatorFactory));

	        _priceCalculatorFactory = priceCalculatorFactory;

	        return this;
        }

        /// <inheritdoc cref="ProcessWithTasks"/>
        public IMatchOptions ProcessWithTasks(int count)
        {
            _taskCount = count;
            return this;
        }

        /// <inheritdoc cref="AdjustInput"/>
        public IInternalMatchOptions AdjustInput(Action<IInputConfiguration> inputConfigurationFactory)
        {
            ContractAssertions.IsNotNull(inputConfigurationFactory, nameof(inputConfigurationFactory));

            _inputConfigurationFactory = inputConfigurationFactory;

            return this;
        }

        /// <inheritdoc cref="SkipUnitsAfterMatch"/>
        public IInternalMatchOptions SkipUnitsAfterMatch(int skip)
        {
            _skipAfterMatch = skip;
            return this;
        }

        /// <inheritdoc cref="HasUnitSize"/>
        public IInternalMatchOptions HasUnitSize(UnitSize unitSize)
        {
            _unitSize = unitSize;
            return this;
        }

        #endregion

        /// <inheritdoc cref="BuildMatchParser"/>
        public IMatchParser BuildMatchParser()
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
                throw new InvalidOperationException("One match finder has no limitations.");

            var matchFinders = new IMatchFinder[_limitFactories.Count];

            for (var i = 0; i < _limitFactories.Count; i++)
            {
                var limit = _limitFactories[i]();
                var matchFinder = _matchFinderFactories[i](options, limit);

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

        private IAdditionalMatchFinder WithinLimitations(Func<FindLimitations> limitFactory)
        {
            ContractAssertions.IsNotNull(limitFactory, nameof(limitFactory));

            _limitFactories ??= new List<Func<FindLimitations>>();
            _limitFactories.Add(limitFactory);

            return this;
        }
    }
}
