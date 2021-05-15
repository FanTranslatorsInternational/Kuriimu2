//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;
//using Komponent.IO;
//using Kontract.Attributes;
//using Kontract.Interfaces;
//using Kontract.Interfaces.Common;
//using Kontract.Interfaces.Text;

//namespace plugin_metal_max.ARR
//{
//    [Export(typeof(ArrAdapter))]
//    [Export(typeof(IPlugin))]
//    [PluginInfo("B6C58C25-4E1C-4B9C-ABCF-DE905B1BBF51", "Metal Max 3: ARR Credits Text", "ARR", "IcySon55, BuddyRoach", "", "This is the Metal Max 3 ARR credits text adapter for Kuriimu2.")]
//    [PluginExtensionInfo("*.arr")]
//    public sealed class ArrAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles, IAddEntries, IDeleteEntries
//    {
//        private ARR _format;

//        #region Properties

//        public IEnumerable<TextEntry> Entries => _format?.Entries;

//        public string NameFilter => @".*";

//        public int NameMaxLength => 0;

//        public string LineEndings { get; set; } = "\n";

//        public bool LeaveOpen { get; set; }

//        #endregion

//        public bool Identify(StreamInfo input)
//        {
//            try
//            {
//                using (var br = new BinaryReaderX(input.FileData, true))
//                {
//                    var count = br.ReadInt32();

//                    var pointers = new List<short>();

//                    for (var i = 0; i < count; i++)
//                    {
//                        for (var j = 0; j < 7; j++)
//                        {
//                            var p = br.ReadInt16();
//                            if (p == 0) continue;
//                            pointers.Add(p);
//                        }
//                    }

//                    return pointers.Max() < br.BaseStream.Length && pointers.Max() >= br.BaseStream.Length - 64;
//                }
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }

//        public void Load(StreamInfo input)
//        {
//            _format = new ARR(input.FileData);
//        }

//        public void Save(StreamInfo output, int versionIndex = 0)
//        {
//            _format.Save(output.FileData);
//        }

//        public void Dispose() { }

//        // IAddEntries
//        public TextEntry NewEntry()
//        {
//            return new TextEntry { Name = (_format.Entries.Count + 1).ToString() };
//        }

//        public bool AddEntry(TextEntry entry)
//        {
//            _format.Entries.Add(entry);
//            return true;
//        }

//        public bool DeleteEntry(TextEntry entry)
//        {
//            _format.Entries.Remove(entry);
//            return true;
//        }
//    }
//}
