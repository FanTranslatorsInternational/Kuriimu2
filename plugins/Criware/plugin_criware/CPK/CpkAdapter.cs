using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.Common;

namespace plugin_criware.CPK
{
    [Export(typeof(CpkAdapter))]
    public sealed class CpkAdapter : IIdentifyFiles, ILoadFiles, ISaveFiles //IArchiveAdapter
    {
        private CPK _format;

        #region Properties

        // Files


        #endregion

        public bool Identify(string filename)
        {
            throw new NotImplementedException();
        }

        void ILoadFiles.Load(string filename)
        {
            throw new NotImplementedException();
        }

        public void Save(string filename, int versionIndex = 0)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
