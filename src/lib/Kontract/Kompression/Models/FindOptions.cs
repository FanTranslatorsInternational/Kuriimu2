using Kontract.Kompression.Interfaces.Configuration;

namespace Kontract.Kompression.Models
{
    /// <summary>
    /// Contains information to configure the search of pattern matches.
    /// </summary>
    public class FindOptions
    {
        /// <summary>
        /// Gets the manipulator for the input.
        /// </summary>
        public IInputManipulator InputManipulator { get; }

        /// <summary>
        /// Gets the amount of units to skipUnits after a match was found.
        /// </summary>
        public int SkipUnitsAfterMatch { get; }

        /// <summary>
        /// Gets the size of a unit.
        /// </summary>
        public UnitSize UnitSize { get; }

        /// <summary>
        /// Gets the number of tasks to use for parallel match finding.
        /// </summary>
        public int TaskCount { get; }

        /// <summary>
        /// Creates a new instance of <see cref="FindOptions"/>.
        /// </summary>
        /// <param name="inputManipulator">The manipulator of the input stream.</param>
        /// <param name="skipUnits">Gets the number of units to skipUnits after a match was found.</param>
        /// <param name="unitSize">Gets the size of a unit.</param>
        /// <param name="taskCount">Gets the number of tasks to use for parallel match finding.</param>
        public FindOptions(IInputManipulator inputManipulator, int skipUnits, UnitSize unitSize, int taskCount)
        {
            InputManipulator = inputManipulator;
            SkipUnitsAfterMatch = skipUnits;
            UnitSize = unitSize;
            TaskCount = taskCount;
        }
    }
}
