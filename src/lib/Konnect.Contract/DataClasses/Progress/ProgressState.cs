namespace Konnect.Contract.DataClasses.Progress;

public class ProgressState
{
    public required double MaxPercentage { get; set; }
    public required double MinPercentage { get; set; }

    public required long PartialValue { get; set; }
    public required long MaxValue { get; set; }

    public string? PreText { get; set; }
    public string? Message { get; set; }
}