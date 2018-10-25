using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace plugin_valkyria_chronicles.SFNT
{
    [Export(typeof(SfntAdapter))]
    [Export(typeof(IImageAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    //[Export(typeof(ISaveFiles))]
    [PluginInfo("A294C965-6BC5-4EC5-8814-D4305115B73A", "VC-SFNT Font", "SFNT", "IcySon55", "", "This is the SFNT image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.bf1")]
    public sealed class SfntAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles//, ISaveFiles
    {
        private SFNT _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        #endregion

        public bool Identify(string filename)
        {
            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                    return br.PeekString() == "SFNT";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _format = new SFNT(File.OpenRead(filename));
                _bitmapInfos = _format.Images.Select((i, index) => new BitmapInfo { Bitmaps = new List<Bitmap> { i }, Name = $"{index}" }).ToList();
            }
        }

        public async Task<bool> Encode(IProgress<ProgressReport> progress)
        {
            // TODO: Get Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
