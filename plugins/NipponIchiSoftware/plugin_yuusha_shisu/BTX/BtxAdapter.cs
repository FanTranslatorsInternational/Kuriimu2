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

        public IList<EncodingInfo> ImageEncodingInfos =>
            BTX.IndexEncodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName)).Union(BTX.Encodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName))).ToList();

        public bool LeaveOpen { get; set; }

        public IList<EncodingInfo> PaletteEncodingInfos =>
            BTX.PaletteEncodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName, 1)).ToList();

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
            {
                var indexEncodingInfo = ImageEncodingInfos.FirstOrDefault(x => x.EncodingIndex == (int)_format.Header.Format);
                var paletteEncodingInfo = PaletteEncodingInfos.FirstOrDefault(x => x.EncodingIndex == (int) _format.Header.Format);
                _bitmapInfos = new List<BitmapInfo>
                {
                    new IndexedBitmapInfo(_format.Texture,
                        indexEncodingInfo,
                        _format.Palette,
                        paletteEncodingInfo)
                };
            }
            else
                _bitmapInfos = new List<BitmapInfo>
                {
                    new BitmapInfo(_format.Texture, ImageEncodingInfos.First(x=>x.EncodingIndex==(int)_format.Header.Format))
                };
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo formatInfo, IProgress<ProgressReport> progress)
        {

            return false;
        }

        public async Task<bool> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress)
        {

            return false;
        }

        public Task<bool> SetColorInPalette(IndexedBitmapInfo info, Color color, int index, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                //progress.Report(new ProgressReport { Message = "Replace color...", Percentage = 0 });

                var enc = BTX.IndexEncodings[info.ImageEncoding.EncodingIndex];

                // Get index list
                var colorList = Kanvas.Kolors.DecomposeImage(info.Image);
                var indices = enc.DecomposeWithPalette(colorList, info.Palette).ToList();

                // Replace color
                info.Palette[index] = color;

                // Compose index list again
                var newColorList = enc.Compose(indices, info.Palette).ToArray();
                info.Image = Kanvas.Kolors.ComposeImage(newColorList, info.Image.Width, info.Image.Height);

                return true;
            });
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
