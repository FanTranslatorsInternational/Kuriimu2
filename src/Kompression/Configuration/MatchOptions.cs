using System;
using System.Collections.Generic;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Configuration
{
    class MatchOptions : IMatchOptions
    {
        internal IList<Func<IList<FindLimitations>, FindOptions, IMatchFinder>> MatchFinderFactories { get; private set; }

        internal IList<Func<FindLimitations>> LimitFactories { get; private set; }

        internal Func<IPriceCalculator> PriceCalculatorFactory { get; private set; }

        internal Func<IList<IMatchFinder>, IPriceCalculator, FindOptions, IMatchParser> MatchParserFactory { get; private set; }

        internal bool? FindBackwards { get; private set; }

        internal int? PreBufferSize { get; private set; }

        internal int? SkipAfterMatch { get; private set; }

        internal UnitSize? UnitSize { get; private set; }

        internal int? TaskCount { get; private set; }

        internal MatchOptions() { }

        /// <inheritdoc cref="CalculatePricesWith"/>
        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            PriceCalculatorFactory = priceCalculatorFactory;
            return this;
        }

        /// <inheritdoc cref="FindMatchesWith"/>
        public IMatchOptions FindMatchesWith(Func<IList<FindLimitations>, FindOptions, IMatchFinder> matchFinderFactory)
        {
            if (MatchFinderFactories == null)
                MatchFinderFactories = new List<Func<IList<FindLimitations>, FindOptions, IMatchFinder>>();

            MatchFinderFactories.Add(matchFinderFactory);

            return this;
        }

        /// <inheritdoc cref="ParseMatchesWith"/>
        public IMatchOptions ParseMatchesWith(Func<IList<IMatchFinder>, IPriceCalculator, FindOptions, IMatchParser> matchParserFactory)
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
            FindBackwards = true;
            return this;
        }

        /// <inheritdoc cref="WithPreBufferSize"/>
        public IMatchOptions WithPreBufferSize(int size)
        {
            PreBufferSize = size;
            return this;
        }

        /// <inheritdoc cref="SkipUnitsAfterMatch"/>
        public IMatchOptions SkipUnitsAfterMatch(int skip)
        {
            SkipAfterMatch = skip;
            return this;
        }

        /// <inheritdoc cref="ProcessWithTasks"/>
        public IMatchOptions ProcessWithTasks(int count)
        {
            TaskCount = count;
            return this;
        }

        /// <inheritdoc cref="WithUnitSize"/>
        public IMatchOptions WithUnitSize(UnitSize unitSize)
        {
            UnitSize = unitSize;
            return this;
        }
    }
}
