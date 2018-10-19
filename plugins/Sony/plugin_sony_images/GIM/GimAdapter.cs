using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace plugin_sony_images.GIM
{
    [Export(typeof(GimAdapter))]
    [Export(typeof(IImageAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ILoadStreams))]
    //[Export(typeof(ISaveFiles))]
    [PluginInfo("1AD809EE-3C0F-4837-98AD-E21EC42F29B8", "GIM File", "GIM", "IcySon55", "", "This is the GIM image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.gim")]
    public sealed class GimAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ILoadStreams //, ISaveFiles
    {
        private GIM _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        #endregion

        public bool Identify(string filename)
        {
            try
            {
                // TODO: Look for potential BE files that might have the "GIM" magic
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                    return br.PeekString(3) == "MIG";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
                Load(File.OpenRead(filename));
        }

        public void Load(params Stream[] inputs)
        {
            if (inputs.Length <= 0)
                throw new Exception("No stream was provided to load from.");

            _format = new GIM(inputs[0]);
            _bitmapInfos = new List<BitmapInfo> { new BitmapInfo { Bitmaps = new List<Bitmap> { _format.Image }, Name = "0" } };
        }

        public async Task<bool> Encode(IProgress<ProgressReport> progress)
        {
            // TODO: Get Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(string filename, int versionIndex = 0)
        {
            //_format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
