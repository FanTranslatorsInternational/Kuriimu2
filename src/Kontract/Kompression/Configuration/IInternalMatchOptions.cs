using System;
using Kontract.Kompression.Model;

namespace Kontract.Kompression.Configuration
{
    public interface IInternalMatchOptions : IMatchOptions
    {
        /// <summary>
        /// Sets the factory to create an instance of <see cref="IMatchParser"/>.
        /// </summary>
        /// <param name="matchParserFactory">The factory to create an instance of <see cref="IMatchParser"/>.</param>
        /// <returns>The option object.</returns>
        IInternalMatchOptions ParseMatchesWith(Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> matchParserFactory);

        /// <summary>
        /// Sets a default parser to parse matches via "Optimal Parsing".
        /// </summary>
        /// <returns>The option object.</returns>
        IInternalMatchOptions ParseOptimal();

        /// <summary>
        /// Add a match finder.
        /// </summary>
        /// <param name="matchFinderFactory">The factory to create an <see cref="IMatchFinder"/>.</param>
        /// <returns>The option object.</returns>
        IMatchLimitations FindWith(Func<FindOptions, FindLimitations, IMatchFinder> matchFinderFactory);

        /// <summary>
        /// Adds a default match finder, which finds sequence patterns.
        /// </summary>
        /// <returns>The option object.</returns>
        IMatchLimitations FindMatches();

        /// <summary>
        /// Adds a default match finder, which finds repeating units.
        /// </summary>
        /// <returns>The option object.</returns>
        IMatchLimitations FindRunLength();

        /// <summary>
        /// Adds a default match finder, which finds repeating units of the given value.
        /// </summary>
        /// <param name="constant">The value to check for repetition.</param>
        /// <returns>The option object.</returns>
        IMatchLimitations FindConstantRunLength(int constant);

        /// <summary>
        /// Sets the factory to create an instance of <see cref="IPriceCalculator"/>.
        /// </summary>
        /// <param name="priceCalculatorFactory">The factory to create an instance of <see cref="IPriceCalculator"/>.</param>
        /// <returns>The option object.</returns>
        IInternalMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory);

        /// <summary>
        /// Sets the factory to create an instance of <see cref="IInputManipulator"/>.
        /// </summary>
        /// <param name="inputConfigurationFactory">The factory to configure the input configuration.</param>
        /// <returns>The option object.</returns>
        IInternalMatchOptions AdjustInput(Action<IInputConfiguration> inputConfigurationFactory);

        /// <summary>
        /// Sets the amount of units to skip after a match.
        /// </summary>
        /// <param name="skip">The amount of units to skip after a match.</param>
        /// <returns>The option object.</returns>
        IInternalMatchOptions SkipUnitsAfterMatch(int skip);

        /// <summary>
        /// Sets the size of a unit to match.
        /// </summary>
        /// <param name="unitSize">The size of a unit to match.</param>
        /// <returns>The option object.</returns>
        IInternalMatchOptions HasUnitSize(UnitSize unitSize);

        /// <summary>
        /// Builds the match parser from the set options.
        /// </summary>
        /// <returns>The newly created match parser.</returns>
        IMatchParser BuildMatchParser();
    }
}
