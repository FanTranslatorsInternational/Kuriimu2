﻿using Kontract.Models.Progress;

namespace Kontract.Interfaces.Progress
{
    public interface IProgressOutput
    {
        void SetProgress(ProgressState state);

        void StartProgress();

        void FinishProgress();
    }
}
