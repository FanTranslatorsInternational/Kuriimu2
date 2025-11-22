namespace Konnect.Contract.Progress;

/// <summary>
/// Exposes methods to report progress to the Kuriimu runtime.
/// </summary>
public interface IProgressContext
{
    /// <summary>
    /// Gets the minimum percentage this context start from. Can be 0.0 minimum.
    /// </summary>
    double MinPercentage { get; }

    /// <summary>
    /// Gets the maximum percentage this context ends at. Can be 100.0 maximum.
    /// </summary>
    double MaxPercentage { get; }

    /// <summary>
    /// Creates a new scope in the current context.
    /// </summary>
    /// <param name="min">The minimum percentage.</param>
    /// <param name="max">The maximum percentage.</param>
    /// <returns></returns>
    IProgressContext CreateScope(double min, double max);

    /// <summary>
    /// Creates a new scope in the current context.
    /// </summary>
    /// <param name="preText">The text to attach before every progress message.</param>
    /// <param name="min">The minimum percentage.</param>
    /// <param name="max">The maximum percentage.</param>
    /// <returns></returns>
    IProgressContext CreateScope(string preText, double min, double max);

    /// <summary>
    /// Creates a new progress with the maximum value in the scopes percentage range.
    /// </summary>
    /// <param name="maxValue">The maximum value for the reporting progress.</param>
    /// <returns>The new progress context with the set max value.</returns>
    ISetMaxProgressContext SetMaxValue(long maxValue);

    /// <summary>
    /// Reports a message and value relative to the set maximum value.
    /// </summary>
    /// <param name="partialValue">The partial value to report in the scope. A completion rate is calculated against the scopes min and max percentage.</param>
    /// <param name="maxValue">The maximum value in the scopes percentage range.</param>
    void ReportProgress(long partialValue, long maxValue);

    /// <summary>
    /// Reports a message and value relative to the set maximum value.
    /// </summary>
    /// <param name="message">The message to report. Gets combined with the set pre-text.</param>
    /// <param name="partialValue">The partial value to report in the scope. A completion rate is calculated against the scopes min and max percentage.</param>
    /// <param name="maxValue">The maximum value in the scopes percentage range.</param>
    void ReportProgress(string message, long partialValue, long maxValue);

    /// <summary>
    /// Signals the output to start a progress flow.
    /// </summary>
    void StartProgress();

    /// <summary>
    /// Determines if the progress report is already running.
    /// </summary>
    /// <returns>If the progress report is already running.</returns>
    bool IsRunning();

    /// <summary>
    /// Signals the output to finish the current progress flow.
    /// </summary>
    void FinishProgress();
}