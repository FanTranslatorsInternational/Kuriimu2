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
    [PluginInfo("plugin_criware", "CriPak", "CPK", "IcySon55, onepiecefreak, unknownbrackets", "", "")]
    [PluginExtensionInfo("*.cpk")]
    public sealed class CpkAdapter : IArchiveAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles, IArchiveReplaceFiles
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
            _format.Save(primaryFile.FileData);
        }

        void IDisposable.Dispose()
        {
            _format = null;
        }
    }
}
