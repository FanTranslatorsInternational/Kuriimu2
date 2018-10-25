using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;
using plugin_sony_images.GIM;

namespace plugin_valkyria_chronicles.MLX0
{
    [Export(typeof(Mlx0Adapter))]
    [Export(typeof(IImageAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("D611A80B-6200-45CB-86CF-3ADE8AF0AD85", "VC-MLX0 Image", "MLX0", "IcySon55", "", "This is the MLX image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.mlx")]
    public sealed class Mlx0Adapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private MLX0 _format;
        private List<GimAdapter> _gims = new List<GimAdapter>();

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos { get; private set; }

        #endregion

        public bool Identify(string filename)
        {
            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                    return br.PeekString() == "IZCA";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (!File.Exists(filename)) return;
            _format = new MLX0(File.OpenRead(filename));

            BitmapInfos = new List<BitmapInfo>();
            var index = 0;
            foreach (var item in _format.ImageStreams)
            {
                var gim = new GimAdapter();
                gim.Load(item.Image);
                foreach (var bi in gim.BitmapInfos)
                {
                    bi.Name = index + " " + item.Name.Replace("_", "__");
                    BitmapInfos.Add(bi);
                    index++;
                }
                _gims.Add(gim);
            }
        }

        public async Task<bool> Encode(IProgress<ProgressReport> progress)
        {
            // TODO: Get Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(string filename, int versionIndex = 0)
        {
            var gimOutput = new MemoryStream();
            //_gim.Save(gimOutput);
            //_format.ImageStream = gimOutput;
            _format.Save(File.Create(filename));
        }

        public void Dispose()
        {
            //_format.ImageStream.Close();
        }
    }
}
