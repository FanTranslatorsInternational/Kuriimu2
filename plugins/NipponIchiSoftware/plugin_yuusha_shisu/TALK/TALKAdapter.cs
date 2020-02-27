//using System.Collections.Generic;
//using System.Composition;
//using Kontract.Attributes;
//using Kontract.FileSystem.Nodes.Abstract;
//using Kontract.FileSystem.Nodes.Physical;
//using Kontract.Interfaces;
//using Kontract.Interfaces.Common;
//using Kontract.Interfaces.Text;

//namespace plugin_yuusha_shisu.TALK
//{
//    [Export(typeof(IPlugin))]
//    [Export(typeof(TALKAdapter))]
//    [PluginInfo("plugin_yuusha_shisu_talk", "Death of a Hero", "TALK", "StorMyu")]
//    [PluginExtensionInfo("*.bin")]
//    public sealed class TALKAdapter : ITextAdapter, ILoadFiles, ISaveFiles
//    {
//        private TALK _format;

//        #region Properties

//        public IEnumerable<TextEntry> Entries => _format?.Entries;

//        public string NameFilter => @".*";

//        public int NameMaxLength => 0;

//        public string LineEndings { get; set; } = "\n";

//        public bool LeaveOpen { get; set; }

//        #endregion

//        /*public bool Identify(StreamInfo input)
//        {
//            try
//            {
//                using (var br = new BinaryReaderX(input.FileData, true))
//                {
//                    var magic = br.ReadString(4);
//                    var fileSize = br.ReadInt32();
//                    return magic == "TEXT" && fileSize == br.BaseStream.Length;
//                }
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }*/

//        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode node)
//        {
//            _format = new TALK(input.FileData);
//        }

//        public void Save(StreamInfo output, PhysicalDirectoryNode node, int versionIndex = 0)
//        {
//            _format.Save(output.FileData);
//        }

//        public void Dispose() { }


//    }
//}
