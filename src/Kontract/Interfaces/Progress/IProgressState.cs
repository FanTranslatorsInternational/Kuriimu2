using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Progress
{
    public class ProgressState
    {
        public double MaxPercentage { get; set; }
        public double MinPercentage { get; set; }

        public long PartialValue { get; set; }
        public long MaxValue { get; set; }

        public string PreText { get; set; }
        public string Message { get; set; }
    }
}
