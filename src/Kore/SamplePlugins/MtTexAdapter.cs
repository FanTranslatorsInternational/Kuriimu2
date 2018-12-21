using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;

namespace Kore.SamplePlugins
{
    [Export(typeof(MtTexAdapter))]
    [Export(typeof(IImageAdapter))]
    //[Export(typeof(IIdentifyFiles))]
    [Export(typeof(ICreateFiles))]
    //[Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [Export(typeof(IMtFrameworkTextureAdapter))]
    [PluginInfo("5D5B51A3-7280-4E90-B02E-E0ABD7C1F005", "MT Framework Texture", "MTTEX", "IcySon55", "", "This is the MTTEX image adapter for Kuriimu.")]
    [PluginExtensionInfo("*.tex")]
    public sealed class MtTexAdapter : IImageAdapter, /*IIdentifyFiles,*/ ICreateFiles, /*ILoadFiles,*/ ISaveFiles, IMtFrameworkTextureAdapter
    {
        private MTTEX _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        #endregion

        public bool Identify(string filename)
        {
            var result = true;

            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                {
                    var magic = br.ReadString(4);
                    if (magic != "TEX\0" && magic != "\0XET")
                        result = false;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void Create()
        {
            //_format = new MTTEX();
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _format = new MTTEX(File.OpenRead(filename));
                _bitmapInfos = new List<BitmapInfo>() { new BitmapInfo { Bitmaps = _format.Bitmaps, Name = "0" } };
            }
        }

        public async Task<bool> Encode(IProgress<ProgressReport> progress)
        {
            // TODO: Get the Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
