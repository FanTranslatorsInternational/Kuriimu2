using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Models
{
    /// <summary>
    /// The ProgressReport class passes completion percentage and messages to the UI.
    /// </summary>
    public class ProgressReport
    {
        /// <summary>
        /// The current progress percentage being reported between 0 and 100.
        /// </summary>
        public double Percentage { get; set; } = 0.0;

        /// <summary>
        /// The current status message for this progress report.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Extra data that may be provided by the async task.
        /// </summary>
        public object Data { get; set; } = null;

        /// <summary>
        /// Simple check for whether or not there is a message.
        /// </summary>
        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    }
}
