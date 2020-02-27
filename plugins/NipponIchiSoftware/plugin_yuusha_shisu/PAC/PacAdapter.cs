//using System;
//using System.Collections.Generic;
//using System.Composition;
//using System.Linq;
//using Komponent.IO;
//using Kontract.Attributes;
//using Kontract.FileSystem.Nodes.Abstract;
//using Kontract.FileSystem.Nodes.Physical;
//using Kontract.Interfaces;
//using Kontract.Interfaces.Archive;
//using Kontract.Interfaces.Common;
//using Kontract.Interfaces.Plugins.Identifier;
//using Kontract.Interfaces.Plugins.State;
//using Kontract.Models.Archive;

//namespace plugin_yuusha_shisu.PAC
//{
//    [Export(typeof(PacAdapter))]
//    [Export(typeof(IPlugin))]
//    [PluginInfo("plugin_yuusha_shisu_pac", "Death of a Hero", "PAC", "StorMyu")]
//    [PluginExtensionInfo("*.pac")]
//    public sealed class PacAdapter : IArchiveAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles, IArchiveReplaceFiles
//    {
//        private PAC _format;

//        #region Properties
//        public List<ArchiveFileInfo> Files => _format?.Files;
//        public bool FileHasExtendedProperties => false;
//        public bool LeaveOpen { get; set; }

//        #endregion

//        public bool Identify(StreamInfo fileInfo, BaseReadOnlyDirectoryNode node)
//        {
//            try
//            {
//                return new BinaryReaderX(fileInfo.FileData, LeaveOpen).ReadString(4) == "ARC\0";
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }

//        void ILoadFiles.Load(StreamInfo fileInfo, BaseReadOnlyDirectoryNode node)
//        {
//            _format = new PAC(fileInfo.FileData);

//            // Tie-in MsgAdapter
//            if (fileInfo.FileName.Contains("msg"))
//                foreach (var afi in _format.Files.Where(afi => afi.FileName.Contains("msg")).Select(afi => afi))
//                    afi.PluginNames = new string[] { "plugin_yuusha_shisu_msg" };
//        }

//        public void Save(StreamInfo primaryFile, PhysicalDirectoryNode node, int versionIndex = 0)
//        {
//            _format.Save(primaryFile.FileData);
//        }

//        void IDisposable.Dispose()
//        {
//            _format = null;
//        }

//    }
//}
