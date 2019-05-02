using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Models;
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

        public Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo formatInfo, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                if (formatInfo.Variant == 0)
                {
                    // new image encoding
                    var infoIndex = BitmapInfos.IndexOf(bitmapInfo);

                    if (BTX.IndexEncodings.ContainsKey(formatInfo.EncodingIndex))
                    {
                        // change to index encoding
                        var settings = new IndexedImageSettings(BTX.IndexEncodings[formatInfo.EncodingIndex], BTX.PaletteEncodings[5], bitmapInfo.Image.Width, bitmapInfo.Image.Height);
                        var (indexData, paletteData) = Kanvas.Kolors.Save(bitmapInfo.Image, settings);
                        var (image, palette) = Kanvas.Kolors.Load(indexData, paletteData, settings);

                        var indexEncodingInfo = ImageEncodingInfos.FirstOrDefault(x => x.EncodingIndex == formatInfo.EncodingIndex);
                        var paletteEncodingInfo = PaletteEncodingInfos.FirstOrDefault(x => x.EncodingIndex == formatInfo.EncodingIndex);
                        BitmapInfos[infoIndex] = new IndexedBitmapInfo(image, indexEncodingInfo, palette, paletteEncodingInfo);

                        _format.Palette = palette;
                        _format.Texture = image;
                        _format.Header.Format = (ImageFormat)formatInfo.EncodingIndex;
                        _format.Header.ColorCount = palette.Count;
                        _format.Header.Height = (short)image.Height;
                        _format.Header.Width = (short)image.Width;
                    }
                    else if (BTX.Encodings.ContainsKey(formatInfo.EncodingIndex))
                    {
                        // change to normal encoding
                        var settings = new ImageSettings(BTX.Encodings[formatInfo.EncodingIndex], bitmapInfo.Image.Width, bitmapInfo.Image.Height);
                        var data = Kanvas.Kolors.Save(bitmapInfo.Image, settings);
                        var image = Kanvas.Kolors.Load(data, settings);

                        var encodingInfo = ImageEncodingInfos.FirstOrDefault(x => x.EncodingIndex == formatInfo.EncodingIndex);
                        BitmapInfos[infoIndex] = new BitmapInfo(image, encodingInfo);

                        _format.Palette = null;
                        _format.Texture = image;
                        _format.Header.Format = (ImageFormat)formatInfo.EncodingIndex;
                        _format.Header.ColorCount = -1;
                        _format.Header.Height = (short)image.Height;
                        _format.Header.Width = (short)image.Width;
                    }
                }
                else
                {
                    // new palette encoding
                    var indexedInfo = (IndexedBitmapInfo)bitmapInfo;

                    var paletteEncoding = BTX.PaletteEncodings[formatInfo.EncodingIndex];
                    var paletteData = paletteEncoding.Save(indexedInfo.Palette);
                    var newPalette = paletteEncoding.Load(paletteData).ToList();

                    indexedInfo.Palette = newPalette;
                    indexedInfo.SetPaletteEncoding(formatInfo);

                    _format.Palette = newPalette;
                    _format.Header.Format = (ImageFormat)formatInfo.EncodingIndex;
                    _format.Header.ColorCount = newPalette.Count;
                }

                return true;
            });
        }

        public async Task<bool> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress)
        {
            //progress.Report(new ProgressReport { Message = "Replace color...", Percentage = 0 });

            var enc = BTX.IndexEncodings[info.ImageEncoding.EncodingIndex];

            // Get index list
            var colorList = Kanvas.Kolors.DecomposeImage(info.Image);
            var indices = enc.DecomposeWithPalette(colorList, info.Palette).ToList();

            // Replace color
            info.Palette = palette;

            // Compose index list again
            var newColorList = enc.Compose(indices, info.Palette).ToArray();
            info.Image = Kanvas.Kolors.ComposeImage(newColorList, info.Image.Width, info.Image.Height);

            return true;
        }

        public async Task<bool> SetColorInPalette(IndexedBitmapInfo info, Color color, int index, IProgress<ProgressReport> progress)
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
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
