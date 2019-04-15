using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;

namespace WinFormsTest
{
    [Export(typeof(IPlugin))]
    [PluginExtensionInfo("*.image")]
    [PluginInfo("Test-Image-Id")]
    public class TestImage : IIdentifyFiles, ILoadFiles, IImageAdapter
    {
        public bool LeaveOpen { get; set; }

        private List<(byte[], ImageSettings)> InternalImageInfo { get; set; }

        public IList<BitmapInfo> BitmapInfos { get; private set; }

        public IList<FormatInfo> FormatInfos => Formats.Select(x => new FormatInfo(x.Key, x.Value.FormatName)).ToList();

        public Dictionary<int, IImageFormat> Formats = new Dictionary<int, IImageFormat>
        {
            [0] = new RGBA(4, 4, 4, 4),
            [1] = new RGBA(8, 8, 8),
            [2] = new LA(4, 4)
        };

        public void Load(StreamInfo file)
        {
            BitmapInfos = new List<BitmapInfo>();
            InternalImageInfo = new List<(byte[], ImageSettings)>();

            using (var br = new BinaryReader(file.FileData, Encoding.ASCII, LeaveOpen))
            {
                var imageCount = br.ReadInt32();
                br.BaseStream.Position += 0x10 - br.BaseStream.Position % 0x10;

                for (int i = 0; i < imageCount; i++)
                {
                    var width = br.ReadInt32();
                    var height = br.ReadInt32();
                    var dataSize = br.ReadInt32();
                    var format = br.ReadInt32();
                    var data = br.ReadBytes(dataSize);

                    var settings = new ImageSettings { Width = width, Height = height, Format = Formats[format] };
                    BitmapInfos.Add(new BitmapInfo(Common.Load(data, settings), new FormatInfo(format, Formats[format].FormatName)));
                    InternalImageInfo.Add((data, settings));

                    br.BaseStream.Position += 0x10 - br.BaseStream.Position % 0x10;
                }
            }
        }

        public void Dispose()
        {
            ;
        }

        public bool Identify(StreamInfo file)
        {
            return true;
        }

        public Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            if (bitmapInfo.Image == null || bitmapInfo.FormatInfo.FormatIndex == formatInfo.FormatIndex)
                return Task.Factory.StartNew(() => false);

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = "Begin of encoding" });

                Thread.Sleep(1000);

                var internalIndex = BitmapInfos.IndexOf(bitmapInfo);
                var newSettings = new ImageSettings { Width = bitmapInfo.Image.Width, Height = bitmapInfo.Image.Height, Format = Formats[formatInfo.FormatIndex] };
                var encoded = Common.Save(bitmapInfo.Image, newSettings);
                InternalImageInfo[internalIndex] = (encoded, newSettings);

                progress.Report(new ProgressReport { Percentage = 50, Message = "Re-encoding finished" });
                Thread.Sleep(1000);

                BitmapInfos[internalIndex].Image = Common.Load(encoded, newSettings);

                progress.Report(new ProgressReport { Percentage = 100, Message = "Reload of image finished" });

                return true;
            });
        }
    }
}
