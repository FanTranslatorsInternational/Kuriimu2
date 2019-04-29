using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Interface;
using Kanvas.Models;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using Kontract.Models;
using Kontract.Models.Image;
using WinFormsTest.Image.Models;

namespace WinFormsTest.Image
{
    [Export(typeof(IPlugin))]
    [PluginExtensionInfo("*.bin")]
    public class TestImage : IImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private static Dictionary<int, IColorEncoding> _encodings = new Dictionary<int, IColorEncoding>()
        {
            [0] = new RGBA(8, 8, 8, 8),
            [1] = new RGBA(8, 8, 8)
        };

        public IList<BitmapInfo> BitmapInfos { get; private set; }

        public IList<EncodingInfo> ImageEncodingInfos { get; private set; } =
            _encodings.Select(x => new EncodingInfo(x.Key, x.Value.FormatName)).ToList();
        public Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo encodingInfo, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                var img = bitmapInfo.Image;

                progress.Report(new ProgressReport { Message = "Re-encode..." });
                var encoded = Kanvas.Kolors.Save(img,
                    new ImageSettings(_encodings[encodingInfo.EncodingIndex], img.Width, img.Height));
                progress.Report(new ProgressReport { Message = "Re-encode...", Percentage = 50 });
                var newImg = Kanvas.Kolors.Load(encoded,
                    new ImageSettings(_encodings[encodingInfo.EncodingIndex], img.Width, img.Height));
                progress.Report(new ProgressReport { Message = "Re-encode...", Percentage = 100 });

                bitmapInfo.Image = newImg;

                return true;
            });
        }

        public void Dispose()
        {
            _encodings = null;
            BitmapInfos = null;
            ImageEncodingInfos = null;
        }

        public bool LeaveOpen { get; set; }
        public void Load(StreamInfo input)
        {
            using (var br = new BinaryReaderX(input.FileData, LeaveOpen))
            {
                var header = br.ReadType<Header>();
                var imageData = br.ReadBytes(header.dataLength);

                var img = Kanvas.Kolors.Load(imageData,
                    new ImageSettings(_encodings[header.format], header.width, header.height));
                var info = new BitmapInfo(img, ImageEncodingInfos.First(x => x.EncodingIndex == header.format));
                BitmapInfos = new List<BitmapInfo>
                {
                    info
                };
            }
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            var info = BitmapInfos.First();
            var imageData = Kanvas.Kolors.Save(info.Image,
                new ImageSettings(_encodings[info.ImageEncoding.EncodingIndex], info.Image.Width, info.Image.Height));

            var header = new Header
            {
                dataLength = imageData.Length,
                format = info.ImageEncoding.EncodingIndex,
                width = info.Image.Width,
                height = info.Image.Height
            };

            using (var bw = new BinaryWriterX(output.FileData, LeaveOpen))
            {
                bw.WriteType(header);
                bw.Write(imageData);
            }
        }

        public bool Identify(StreamInfo file)
        {
            using (var br = new BinaryReaderX(file.FileData, LeaveOpen))
                return br.ReadString(8) == "IMG TEST";
        }
    }
}
