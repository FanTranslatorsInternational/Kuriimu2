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
    [Export(typeof(IPlugin))]
    [PluginInfo("0337C082-324C-46C2-ABDA-CBD873864D75", "VC-HTEX Image", "HTEX", "IcySon55", "", "This is the HTX image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.htx")]
    public sealed class HtexAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private HTEX _format;
        private GimAdapter _gim = new GimAdapter();

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos { get; private set; }

        public IList<FormatInfo> FormatInfos => throw new NotImplementedException();

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString() == "HTEX";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new HTEX(input.FileData);
            _gim.Load(_format.ImageStream);
            BitmapInfos = _gim.BitmapInfos;
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            var gimOutput = new MemoryStream();
            _gim.Save(gimOutput);
            _format.ImageStream = gimOutput;
            _format.Save(output.FileData);
        }

        public void Dispose()
        {
            _format.ImageStream.Close();
        }
    }
}
