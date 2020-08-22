namespace Kontract.Kompression.Model
{
    /// <summary>
    /// Contains information to configure the search of pattern matches.
    /// </summary>
    public class FindOptions
    {
        /// <summary>
        /// Indicates if a match is searched from end to beginning of the data.
        /// </summary>
        public bool SearchBackwards { get; }

        /// <summary>
        /// Gets the size of a buffer that must come before the first searched position.
        /// </summary>
        public int PreBufferSize { get; }

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
        /// <param name="searchBackwards">Indicates if a match is searched from end to beginning of the data.</param>
        /// <param name="preBufferSize">Gets the size of a buffer that must come before the first searched position.</param>
        /// <param name="skipUnits">Gets the number of units to skipUnits after a match was found.</param>
        /// <param name="unitSize">Gets the size of a unit.</param>
        /// <param name="taskCount">Gets the number of tasks to use for parallel match finding.</param>
        public FindOptions(bool searchBackwards, int preBufferSize, int skipUnits, UnitSize unitSize, int taskCount)
        {
            SearchBackwards = searchBackwards;
            PreBufferSize = preBufferSize;
            SkipUnitsAfterMatch = skipUnits;
            UnitSize = unitSize;
            TaskCount = taskCount;
        }
    }
}
