namespace Kompression.LempelZiv.MatchFinder
{
    /// <summary>
    /// A finder for matches and its basic properties.
    /// </summary>
    public interface IMatchFinder
    {
        /// <summary>
        /// The minimum size a match must have.
        /// </summary>
        int MinMatchSize { get; }

        /// <summary>
        /// The maximum size a match must have.
        /// </summary>
        int MaxMatchSize { get; }
    }
}
