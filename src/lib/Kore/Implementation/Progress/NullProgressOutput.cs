using Kontract.Interfaces.Progress;
using Kontract.Models.Progress;

namespace Kore.Implementation.Progress
{
    /// <summary>
    /// A progress output, which discards all progress.
    /// </summary>
    public class NullProgressOutput : IProgressOutput
    {
        public void SetProgress(ProgressState state)
        {
        }

        public void StartProgress()
        {
        }

        public void FinishProgress()
        {
        }
    }
}
