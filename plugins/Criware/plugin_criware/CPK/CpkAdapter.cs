using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;

namespace plugin_criware.CPK
{
    [Export(typeof(CpkAdapter))]
    [Export(typeof(IPlugin))]
    [Export(typeof(IArchiveAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("AE94898C-6FE4-415F-A34C-1154FC40FB28", "CriPak", "CPK", "IcySon55, onepiecefreak, unknownbrackets", "", "")]
    [PluginExtensionInfo("*.cpk")]
    public sealed class CpkAdapter : IPlugin, IArchiveAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private CPK _format;

        #region Properties

        // Files
        public List<ArchiveFileInfo> Files => _format?.Files;

        public bool FileHasExtendedProperties => false;

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo fileInfo)
        {
            try
            {
                return new BinaryReaderX(fileInfo.FileData, LeaveOpen).ReadString(4) == "CPK ";
            }
            catch (Exception)
            {
                return false;
            }
        }

        void ILoadFiles.Load(StreamInfo fileInfo)
        {
            _format = new CPK(fileInfo.FileData);
        }

        public void Save(StreamInfo primaryFile, int versionIndex = 0)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            _format = null;
        }
    }
}
