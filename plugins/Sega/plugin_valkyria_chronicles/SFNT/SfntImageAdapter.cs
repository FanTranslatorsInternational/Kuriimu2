using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;

namespace plugin_valkyria_chronicles.SFNT
{
    [Export(typeof(SfntImageAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("A294C965-6BC5-4EC5-8814-D4305115B73A", "VC-SFNT Font Image", "SFNT", "IcySon55", "", "This is the SFNT image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.bf1")]
    public sealed class SfntImageAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles
    {
        private SFNT _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public IList<FormatInfo> FormatInfos => throw new NotImplementedException();

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString() == "SFNT";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new SFNT(input.FileData);
            _bitmapInfos = _format.Images.Select((i, index) => new BitmapInfo(i, new FormatInfo(0, "")) { Name = $"{index}" }).ToList();
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
