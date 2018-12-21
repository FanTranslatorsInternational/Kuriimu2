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
    [Export(typeof(BclimAdapter))]
    [Export(typeof(IImageAdapter))]
    //[Export(typeof(IIdentifyFiles))]
    [Export(typeof(ICreateFiles))]
    //[Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("FAD19315-1A30-44A3-B0D4-0E6A8E71A39F", "NW4C BCLIM Image", "BCLIM", "IcySon55", "", "This is the BCLIM image adapter for Kuriimu.")]
    [PluginExtensionInfo("*.bclim")]
    public sealed class BclimAdapter : IImageAdapter, /*IIdentifyFiles,*/ ICreateFiles, /*ILoadFiles,*/ ISaveFiles
    {
        private BCLIM _format;
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
                    if (br.BaseStream.Length < 0x28) return false;

                    br.BaseStream.Position = br.BaseStream.Length - 0x28;
                    if (br.ReadString(4) != "CLIM")
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
            _format = new BCLIM();
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _format = new BCLIM(File.OpenRead(filename));
                _bitmapInfos = new List<BitmapInfo>() { new BitmapInfo { Bitmaps = new List<System.Drawing.Bitmap> { _format.Texture }, Name = "0" } };
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
