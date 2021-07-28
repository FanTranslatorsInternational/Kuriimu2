namespace Kontract.Kompression.Configuration
{
    /// <summary>
    /// Provides functionality to the configure pattern match operations.
    /// </summary>
    public interface IMatchOptions
    {
        /// <summary>
        /// Sets the number of tasks to use to find pattern matches.
        /// </summary>
        /// <param name="count">The number of tasks.</param>
        /// <returns>The option objects.</returns>
        IMatchOptions ProcessWithTasks(int count);
    }
}
