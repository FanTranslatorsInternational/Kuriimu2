using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.IndexEncoding;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Quantization.Ditherers.ErrorDiffusion;
using Kanvas.Quantization.Quantizers;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using Kontract.Models;
using Kontract.Models.Image;
using WinFormsTest.Image.Models;
using EncodingInfo = Kontract.Models.Image.EncodingInfo;

namespace WinFormsTest.Image
{
    [Export(typeof(IPlugin))]
    [PluginExtensionInfo("*.bin")]
    public class TestIndexImage : IIndexedImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private static Dictionary<int, IIndexEncoding> _indexEncodings = new Dictionary<int, IIndexEncoding>()
        {
            [0] = new Index(8, false)
        };
        private static Dictionary<int, IColorEncoding> _encodings = new Dictionary<int, IColorEncoding>()
        {
            [0] = new RGBA(8, 8, 8, 8),
            [1] = new RGBA(8, 8, 8)
        };

        public IList<BitmapInfo> BitmapInfos { get; private set; }
        public IList<EncodingInfo> ImageEncodingInfos { get; private set; } =
            _indexEncodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName)).ToList();

        public Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo encodingInfo, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                var info = (IndexedBitmapInfo)bitmapInfo;
                var img = info.Image;

                IndexImageSettings settings;
                settings = encodingInfo.Variant == 0
                    ? new IndexImageSettings(_indexEncodings[encodingInfo.EncodingIndex],
                        _encodings[info.PaletteEncoding.EncodingIndex], img.Width, img.Height)
                    : new IndexImageSettings(_indexEncodings[info.ImageEncoding.EncodingIndex],
                        _encodings[encodingInfo.EncodingIndex], img.Width, img.Height);
                settings.QuantizationSettings = new QuantizationSettings(new DistinctSelectionQuantizer(), img.Width, img.Height)
                {
                    ColorCount = info.ColorCount,
                    Ditherer = new FloydSteinbergDitherer()
                };

                progress.Report(new ProgressReport { Message = "Re-encode..." });
                var (indexData, paletteData) = Kanvas.Kolors.Save(img, settings);

                progress.Report(new ProgressReport { Message = "Re-encode...", Percentage = 50 });
                var newImg = Kanvas.Kolors.Load(indexData, paletteData, settings);

                progress.Report(new ProgressReport { Message = "Re-encode...", Percentage = 100 });

                info.Image = newImg.image;
                info.Palette = newImg.palette;
                if (encodingInfo.Variant == 0)
                    info.SetImageEncoding(encodingInfo);
                else
                    info.SetPaletteEncoding(encodingInfo);

                return true;
            });
        }

        public IList<EncodingInfo> PaletteEncodingInfos { get; private set; } =
            _encodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName, 1)).ToList();

        public Task<bool> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                var enc = _indexEncodings[info.ImageEncoding.EncodingIndex];

                progress.Report(new ProgressReport { Message = "Decompose image...", Percentage = 0 });

                var colorList = Kanvas.Kolors.DecomposeImage(info.Image);
                var data = enc.Decompose(colorList);

                progress.Report(new ProgressReport { Message = "Compose image...", Percentage = 50 });

                var newColorList = enc.Compose(data.indices, palette);
                var newImg = Kanvas.Kolors.ComposeImage(newColorList.ToList(), info.Image.Width, info.Image.Height);

                progress.Report(new ProgressReport { Message = "Done.", Percentage = 100 });

                info.Image = newImg;
                info.Palette = palette;

                return true;
            });
        }

        public Task<bool> SetColorInPalette(IndexedBitmapInfo info, Color color, int index, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                //progress.Report(new ProgressReport { Message = "Replace color...", Percentage = 0 });

                var enc = _indexEncodings[info.ImageEncoding.EncodingIndex];

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

        public void Dispose()
        {
            BitmapInfos = null;
            ImageEncodingInfos = null;
        }

        public bool LeaveOpen { get; set; }
        public void Load(StreamInfo input)
        {
            using (var br = new BinaryReaderX(input.FileData, LeaveOpen))
            {
                var header = br.ReadType<IndexHeader>();
                var paletteData = br.ReadBytes(header.paletteLength);
                var imgData = br.ReadBytes(header.dataLength);

                var settings = new IndexImageSettings(_indexEncodings[header.imageFormat], _encodings[header.paletteFormat], header.width, header.height);
                var img = Kanvas.Kolors.Load(imgData, paletteData, settings);
                var info = new IndexedBitmapInfo(img.image, ImageEncodingInfos[header.imageFormat], img.palette, PaletteEncodingInfos[header.paletteFormat]);

                BitmapInfos = new List<BitmapInfo> { info };
            }
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            var info = BitmapInfos.Cast<IndexedBitmapInfo>().First();
            var settings = new IndexImageSettings(_indexEncodings[info.ImageEncoding.EncodingIndex], _encodings[info.PaletteEncoding.EncodingIndex], info.Image.Width, info.Image.Height);
            var data = Kanvas.Kolors.Save(info.Image, settings);

            var header = new IndexHeader
            {
                paletteLength = data.paletteData.Length,
                colorCount = info.Palette.Count,
                dataLength = data.indexData.Length,
                paletteFormat = info.PaletteEncoding.EncodingIndex,
                imageFormat = info.ImageEncoding.EncodingIndex,
                width = info.Image.Width,
                height = info.Image.Height
            };

            using (var bw = new BinaryWriterX(output.FileData, LeaveOpen))
            {
                bw.WriteType(header);
                bw.Write(data.paletteData);
                bw.Write(data.indexData);
            }
        }

        public bool Identify(StreamInfo file)
        {
            using (var br = new BinaryReaderX(file.FileData, LeaveOpen))
                return br.ReadString(8) == "IIMGTEST";
        }
    }
}
