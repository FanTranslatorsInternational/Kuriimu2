using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kore.Managers;
using Serilog;

namespace Kore.Models
{
    class SaveInfo
    {
        public IProgressContext Progress { get; set; }

        public IDialogManager DialogManager { get; set; }

        public ILogger Logger { get; set; }
    }
}
