using System;
using System.Collections.Generic;
using System.Composition;
using System.Drawing;
using System.Linq;
using Kanvas.Models;
using Kanvas.Quantization.Quantizers;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
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
    public class BtxAdapter : BaseIndexedImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private BTX _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public override IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public override IList<EncodingInfo> ImageEncodingInfos { get; } =
            BTX.IndexEncodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName, true))
                .Union(BTX.Encodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName))).ToList();

        public override IList<EncodingInfo> PaletteEncodingInfos { get; } =
            BTX.PaletteEncodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName)).ToList();

        public bool LeaveOpen { get; set; }

        #endregion

        #region Transcoding

        protected override Bitmap Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, IProgress<ProgressReport> progress)
        {
            var img = bitmapInfo.Image;
            var settings = new ImageSettings(BTX.Encodings[imageEncoding.EncodingIndex], img.Width, img.Height);
            var data = Kanvas.Kolors.Save(img, settings);
            _format.Texture = Kanvas.Kolors.Load(data, settings);
            return _format.Texture;
        }

        protected override (Bitmap Image, IList<Color> Palette) Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, EncodingInfo paletteEncoding, IProgress<ProgressReport> progress)
        {
            var img = bitmapInfo.Image;
            var indexEncoding = BTX.IndexEncodings[imageEncoding.EncodingIndex];
            var palEncoding = BTX.PaletteEncodings[paletteEncoding.EncodingIndex];
            var settings = new IndexedImageSettings(indexEncoding, palEncoding, img.Width, img.Height)
            {
                QuantizationSettings = new QuantizationSettings(new WuColorQuantizer(6, 3), img.Width, img.Height)
                {
                    ParallelCount = 8
                }
            };
            var data = Kanvas.Kolors.Save(img, settings);
            (_format.Texture, _format.Palette) = Kanvas.Kolors.Load(data.indexData, data.paletteData, settings);
            return (_format.Texture, _format.Palette);
        }

        protected override Bitmap TranscodeWithPalette(IndexedBitmapInfo bitmapInfo, IList<Color> palette, IProgress<ProgressReport> progress)
        {
            var img = bitmapInfo.Image;
            var indexInfo = bitmapInfo as IndexedBitmapInfo;
            var indexEncoding = BTX.IndexEncodings[bitmapInfo.ImageEncoding.EncodingIndex];

            var colorList = Kanvas.Kolors.DecomposeImage(img);
            var indices = indexEncoding.DecomposeWithPalette(colorList, indexInfo?.Palette);
            var newColorList = indexEncoding.Compose(indices, palette).ToList();
            _format.Texture = Kanvas.Kolors.ComposeImage(newColorList, img.Width, img.Height);
            _format.Palette = palette;

            return _format.Texture;
        }

        #endregion

        public bool Identify(StreamInfo input, BaseReadOnlyDirectoryNode node)
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

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode node)
        {
            _format = new BTX(input.FileData);

            if (_format.HasPalette)
            {
                var indexEncodingInfo = ImageEncodingInfos.FirstOrDefault(x => x.EncodingIndex == (int)_format.Header.Format);
                var paletteEncodingInfo = PaletteEncodingInfos.FirstOrDefault(x => x.EncodingIndex == (int)_format.Header.Format);
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

        public void Save(StreamInfo output, PhysicalDirectoryNode node, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
