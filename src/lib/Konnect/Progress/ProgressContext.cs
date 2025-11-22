using Konnect.Contract.DataClasses.Progress;
using Konnect.Contract.Progress;

namespace Konnect.Progress;

public class ProgressContext : ISetMaxProgressContext
{
    private readonly IProgressOutput _output;
    private readonly ProgressState _state;

    private readonly object _lock = new object();
    private bool _isRunning;

    public string PreText { get; }
    public double MinPercentage { get; }
    public double MaxPercentage { get; } = 100.0;
    public long MaxValue { get; private set; } = -1;

    public ProgressContext(IProgressOutput output)
    {
        _output = output;
        _state = new ProgressState
        {
            MinPercentage = 0,
            MaxPercentage = 100.0,
            PartialValue = 0,
            MaxValue = -1
        };
    }

    public ProgressContext(double min, double max, IProgressOutput output) :
        this(output)
    {
        if (min > max)
            throw new InvalidOperationException($"The min value ({min}) has to be smaller than the max value ({max}).");

        MinPercentage = Math.Max(0, min);
        MaxPercentage = Math.Min(100.0, max);

        _state.MinPercentage = MinPercentage;
        _state.MaxPercentage = MaxPercentage;
    }

    public ProgressContext(string preText, double min, double max, IProgressOutput output) :
        this(min, max, output)
    {
        PreText = preText;

        _state.PreText = preText;
    }

    public IProgressContext CreateScope(double min, double max) =>
        CreateScope(null, min, max);

    public IProgressContext CreateScope(string preText, double min, double max)
    {
        if (min < MinPercentage)
            throw new ArgumentOutOfRangeException(nameof(min));
        if (max > MaxPercentage)
            throw new ArgumentOutOfRangeException(nameof(max));

        return new ProgressContext(preText, min, max, _output);
    }

    public ISetMaxProgressContext SetMaxValue(long maxValue)
    {
        MaxValue = _state.MaxValue = maxValue;
        return this;
    }

    public void ReportProgress(string message, long partialValue)
    {
        lock (_lock)
        {
            if (!_isRunning)
                return;
        }

        _state.PartialValue = partialValue;
        _state.Message = message;

        _output.SetProgress(_state);
    }

    public void ReportProgress(long partialValue, long maxValue)
    {
        ReportProgress(_state.Message, partialValue, maxValue);
    }

    public void ReportProgress(string message, long partialValue, long maxValue)
    {
        lock (_lock)
        {
            if (!_isRunning)
                return;
        }

        _state.PartialValue = partialValue;
        _state.MaxValue = maxValue;
        _state.Message = message;

        _output.SetProgress(_state);
    }

    public void StartProgress()
    {
        lock (_lock)
        {
            if (_isRunning)
                return;

            _isRunning = true;
        }

        _output.StartProgress();
    }

    public bool IsRunning()
    {
        lock (_lock)
            return _isRunning;
    }

    public void FinishProgress()
    {
        lock (_lock)
        {
            if (!_isRunning)
                return;

            _isRunning = false;
        }

        _output.FinishProgress();
    }
}