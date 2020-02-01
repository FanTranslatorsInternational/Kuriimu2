using System;
using System.Collections.Generic;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Configuration
{
    /// <summary>
    /// Contains information on configuring match finding and parsing.
    /// </summary>
    class MatchOptions : IMatchOptions
    {
        /// <summary>
        /// Indicates whether to search matches from the beginning to the end of data.
        /// </summary>
        private bool _findBackwards;

        /// <summary>
        /// Gets the size of a buffer located before the first position to search from.
        /// </summary>
        private int _preBufferSize;

        /// <summary>
        /// Gets the amount of units to skip after a match.
        /// </summary>
        private int _skipAfterMatch;

        /// <summary>
        /// Gets the size of a unit to match.
        /// </summary>
        private UnitSize _unitSize = UnitSize.Byte;

        /// <summary>
        /// Gets the number of tasks to use to find pattern matches.
        /// </summary>
        private int _taskCount = Environment.ProcessorCount;

        /// <summary>
        /// The factory to create a list of <see cref="IMatchFinder"/>s.
        /// </summary>
        internal IList<Func<FindLimitations[], FindOptions, IMatchFinder>> MatchFinderFactories { get; private set; }

        /// <summary>
        /// The factory to create a list of <see cref="FindLimitations"/>.
        /// </summary>
        internal IList<Func<FindLimitations>> LimitFactories { get; private set; }

        /// <summary>
        /// The factory to create an <see cref="IPriceCalculator"/>.
        /// </summary>
        internal Func<IPriceCalculator> PriceCalculatorFactory { get; private set; }

        /// <summary>
        /// The factory to create an <see cref="IMatchParser"/>.
        /// </summary>
        internal Func<IMatchFinder[], IPriceCalculator, FindOptions, IMatchParser> MatchParserFactory { get; private set; }

        /// <inheritdoc cref="CalculatePricesWith"/>
        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            PriceCalculatorFactory = priceCalculatorFactory;
            return this;
        }

        /// <inheritdoc cref="FindMatchesWith"/>
        public IMatchOptions FindMatchesWith(Func<FindLimitations[], FindOptions, IMatchFinder> matchFinderFactory)
        {
            if (MatchFinderFactories == null)
                MatchFinderFactories = new List<Func<FindLimitations[], FindOptions, IMatchFinder>>();

            MatchFinderFactories.Add(matchFinderFactory);

            return this;
        }

        /// <inheritdoc cref="ParseMatchesWith"/>
        public IMatchOptions ParseMatchesWith(Func<IMatchFinder[], IPriceCalculator, FindOptions, IMatchParser> matchParserFactory)
        {
            MatchParserFactory = matchParserFactory;
            return this;
        }

        /// <inheritdoc cref="WithinLimitations"/>
        public IMatchOptions WithinLimitations(Func<FindLimitations> limitFactory)
        {
            if (LimitFactories == null)
                LimitFactories = new List<Func<FindLimitations>>();

            LimitFactories.Add(limitFactory);

            return this;
        }

        /// <inheritdoc cref="FindInBackwardOrder"/>
        public IMatchOptions FindInBackwardOrder()
        {
            _findBackwards = true;
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

        /// <inheritdoc cref="BuildOptions"/>
        public FindOptions BuildOptions()
        {
            return new FindOptions(_findBackwards, _preBufferSize, _skipAfterMatch, _unitSize, _taskCount);
        }
    }
}
