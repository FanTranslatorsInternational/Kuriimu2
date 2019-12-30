namespace Kontract.Interfaces
{
    /// <summary>
    /// Exposes methods to report progress to the Kuriimu runtime.
    /// </summary>
    public interface IKuriimuProgress
    {
        /// <summary>
        /// Reports a message and completion rate.
        /// </summary>
        /// <param name="message">The message to report.</param>
        /// <param name="completion">The completion rate to report.</param>
        void Report(string message, double completion);
    }
}
