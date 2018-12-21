using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Common
{
    public interface IRequestFiles
    {
        event EventHandler<RequestFileEventArgs> RequestFile;
    }

    public class RequestFileEventArgs : EventArgs
    {
        public string FileName;
        public StreamInfo SelectedStreamInfo = null;
    }
}
