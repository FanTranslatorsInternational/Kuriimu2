using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using plugin_sony_images.GIM;

namespace plugin_valkyria_chronicles.HTEX
{
    [Export(typeof(HtexAdapter))]
    [Export(typeof(IImageAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("0337C082-324C-46C2-ABDA-CBD873864D75", "VC-HTEX Image", "HTEX", "IcySon55", "", "This is the HTX image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.htx")]
    public sealed class HtexAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private HTEX _format;
        private GimAdapter _gim = new GimAdapter();

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos { get; private set; }

        #endregion

        public bool Identify(string filename)
        {
            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                    return br.PeekString() == "HTEX";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (!File.Exists(filename)) return;
            _format = new HTEX(File.OpenRead(filename));
            _gim.Load(_format.ImageStream);
            BitmapInfos = _gim.BitmapInfos;
        }

        public async Task<bool> Encode(IProgress<ProgressReport> progress)
        {
            // TODO: Get Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(string filename, int versionIndex = 0)
        {
            var gimOutput = new MemoryStream();
            _gim.Save(gimOutput);
            _format.ImageStream = gimOutput;
            _format.Save(File.Create(filename));
        }

        public void Dispose()
        {
            _format.ImageStream.Close();
        }
    }
}
