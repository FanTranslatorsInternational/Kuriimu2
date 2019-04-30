using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using Kontract.Models;
using Kontract.Models.Image;

namespace plugin_yuusha_shisu.BTX
{
    [Export(typeof(BtxAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("plugin_yuusha_shisu_btx", "Death of a Hero", "BTX", "IcySon55")]
    [PluginExtensionInfo("*.btx")]
    public class BtxAdapter : IImageAdapter, IIndexedImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private BTX _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public IList<EncodingInfo> ImageEncodingInfos => BTX.Encodings.Select(x => new EncodingInfo((int)x.Key, x.Value.FormatName)).Union(BTX.IndexEncodings.Select(x => new EncodingInfo((int)x.Key, x.Value.FormatName))).ToList();

        public bool LeaveOpen { get; set; }

        public IList<EncodingInfo> PaletteEncodingInfos => throw new NotImplementedException();

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

            if (_format.HasPalette)
                _bitmapInfos = new List<BitmapInfo> { new IndexedBitmapInfo(_format.Texture, new EncodingInfo((int)_format.Header.Format, _format.FormatName), _format.Palette, new EncodingInfo(0, _format.PaletteFormatName)) };
            else
                _bitmapInfos = new List<BitmapInfo> { new BitmapInfo(_format.Texture, new EncodingInfo((int)_format.Header.Format, _format.FormatName)) };
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo formatInfo, IProgress<ProgressReport> progress)
        {

            return false;
        }

        public async Task<bool> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress)
        {

            return false;
        }

        public async Task<bool> SetColorInPalette(IndexedBitmapInfo info, Color color, int index, IProgress<ProgressReport> progress)
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
