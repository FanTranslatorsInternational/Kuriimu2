namespace Konnect.Contract.Progress;

public interface ISetMaxProgressContext : IProgressContext
{
    /// <summary>
    /// The maximum value for reporting progress.
    /// </summary>
    long MaxValue { get; }

    /// <summary>
    /// Reports a message and value relative to the set maximum value.
    /// </summary>
    /// <param name="message">The message to report. Gets combined with the set pre-text.</param>
    /// <param name="partialValue">The partial value to report in the scope. A completion rate is calculated against the scopes min and max percentage.</param>
    void ReportProgress(string message, long partialValue);
}