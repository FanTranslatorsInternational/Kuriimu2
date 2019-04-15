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

namespace plugin_yuusha_shisu.BTX
{

    [Export(typeof(BtxAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("plugin_yuusha_shisu_btx", "Death of a Hero", "BTX", "IcySon55")]
    [PluginExtensionInfo("*.btx")]
    public class BtxAdapter : IImageAdapter, /*IIndexedImageAdapter,*/ IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private BTX _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public IList<FormatInfo> FormatInfos => BTX.Formats.Select(x => new FormatInfo((int)x.Key, x.Value.FormatName)).Union(BTX.PaletteFormats.Select(x => new FormatInfo((int)x.Key, x.Value.FormatName))).ToList();

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString(4) == "btx\0";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new BTX(input.FileData);
            _bitmapInfos = new List<BitmapInfo> { new BitmapInfo(_format.Texture, new FormatInfo((int)_format.Header.Format, _format.FormatName)) };
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {





            return false;
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
