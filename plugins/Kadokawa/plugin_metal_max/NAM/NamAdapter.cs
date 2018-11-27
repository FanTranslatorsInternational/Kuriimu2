using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace plugin_metal_max.NAM
{
    [Export(typeof(ITextAdapter))]
    [Export(typeof(ITextAdapter))]
    //[Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("18F83F20-3FB7-46DC-9028-EE00D658F029", "Metal Max 3: NAM Text", "NAM", "IcySon55", "", "This is the Metal Max 3 NAM text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.nam")]
    public sealed class NamAdapter : ITextAdapter, ILoadFiles, ISaveFiles
    {
        private NAM _format;

        #region Properties

        public IEnumerable<TextEntry> Entries => _format?.Entries;

        public string NameFilter => @".*";
        public int NameMaxLength => 0;

        public string LineEndings
        {
            get => "\n";
            set => throw new NotImplementedException();
        }

        #endregion

        //public bool Identify(string filename)
        //{
        //    try
        //    {
        //        using (var br = new BinaryReaderX(File.OpenRead(filename)))
        //        {
        //            var count = br.ReadInt32();

        //            var pointers = new List<short>();

        //            for (var i = 0; i < count; i++)
        //            {
        //                for (var j = 0; j < 7; j++)
        //                {
        //                    var p = br.ReadInt16();
        //                    if (p == 0) continue;
        //                    pointers.Add(p);
        //                }
        //            }

        //            return pointers.Last() < br.BaseStream.Length && pointers.Last() >= br.BaseStream.Length - 64;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        public void Load(string filename)
        {
            var arrFile = Path.ChangeExtension(filename, ".ARR");

            if (File.Exists(filename) && File.Exists(arrFile))
                _format = new NAM(File.OpenRead(filename), File.OpenRead(arrFile), filename);
            else if (File.Exists(filename))
                _format = new NAM(File.OpenRead(filename), null, filename);
        }

        public void Save(string filename, int versionIndex = 0)
        {
            var arrFile = Path.ChangeExtension(filename, ".ARR");

            _format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
