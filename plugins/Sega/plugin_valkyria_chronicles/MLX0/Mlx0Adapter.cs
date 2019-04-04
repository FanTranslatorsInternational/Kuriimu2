using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using plugin_sony_images.GIM;

namespace plugin_valkyria_chronicles.MLX0
{
    [Export(typeof(Mlx0Adapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("D611A80B-6200-45CB-86CF-3ADE8AF0AD85", "VC-MLX0 Image", "MLX0", "IcySon55", "", "This is the MLX image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.mlx")]
    public sealed class Mlx0Adapter : IImageAdapter, IIdentifyFiles, ILoadFiles//, ISaveFiles
    {
        private MLX0 _format;
        private List<GimAdapter> _gims = new List<GimAdapter>();

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos { get; private set; }

        public IList<FormatInfo> FormatInfos => throw new NotImplementedException();

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString() == "IZCA";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new MLX0(input.FileData);

            BitmapInfos = new List<BitmapInfo>();
            var index = 0;
            foreach (var (image, name) in _format.ImageStreams)
            {
                var gim = new GimAdapter();
                gim.Load(image);
                foreach (var bi in gim.BitmapInfos)
                {
                    bi.Name = index + (" " + name).Trim();
                    BitmapInfos.Add(bi);
                    index++;
                }
                _gims.Add(gim);
            }
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            var gimOutput = new MemoryStream();
            //_gim.Save(gimOutput);
            //_format.ImageStream = gimOutput;
            _format.Save(output.FileData);
        }

        public void Dispose()
        {
            //_format.ImageStream.Close();
        }
    }
}
