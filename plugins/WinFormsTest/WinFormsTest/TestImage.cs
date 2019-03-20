using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
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

        public IList<FormatInfo> FormatInfos { get; private set; }

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
                    BitmapInfos.Add(new BitmapInfoInternal { Image = Common.Load(data, settings) });
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

        public Task<bool> Encode(BitmapInfo bitmapInfo, IProgress<ProgressReport> progress)
        {
            if (bitmapInfo.Image == null || bitmapInfo.MipMapCount <= 0)
                return Task.Factory.StartNew(() => false);

            return Task.Factory.StartNew(() =>
            {
                    var internalIndex = BitmapInfos.IndexOf(bitmapInfo);
                    var newSettings = new ImageSettings { Width = bitmapInfo.Image.Width, Height = bitmapInfo.Image.Height, Format = Formats[(bitmapInfo as BitmapInfoInternal).FormatIndex] };
                    var encoded = Common.Save(bitmapInfo.Image, newSettings);
                    InternalImageInfo[internalIndex] = (encoded, newSettings);

                    BitmapInfos[internalIndex].Image = Common.Load(encoded, newSettings);

                return true;
            });
        }

        private class BitmapInfoInternal : BitmapInfo
        {
            [Category("Properties")]
            [ReadOnly(true)]
            public int FormatIndex;
        }
    }
}
