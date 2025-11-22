using Konnect.Contract.DataClasses.Progress;

namespace Konnect.Contract.Progress;

public interface IProgressOutput
{
    void SetProgress(ProgressState state);

    void StartProgress();

    void FinishProgress();
}