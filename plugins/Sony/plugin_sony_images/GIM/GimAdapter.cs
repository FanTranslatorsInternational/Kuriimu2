using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;

namespace plugin_sony_images.GIM
{
    [Export(typeof(GimAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("1AD809EE-3C0F-4837-98AD-E21EC42F29B8", "Graphic Image Map", "GIM", "IcySon55", "", "This is the GIM image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.gim")]
    public sealed class GimAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ILoadStreams, ISaveFiles, ISaveStreams
    {
        private GIM _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public IList<FormatInfo> FormatInfos => throw new NotImplementedException();

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                // TODO: Look for potential BE files that might have the "GIM" magic
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString(3) == "MIG";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            Load(input.FileData);
        }

        public void Load(params Stream[] inputs)
        {
            if (inputs.Length <= 0)
                throw new Exception("No stream was provided to load from.");

            _format = new GIM(inputs[0]);
            _bitmapInfos = _format.Images.Select((i, index) => new BitmapInfo(i.First().Item1, new FormatInfo(0, "")) { Name = $"{index}", MipMaps = i.Select(p => p.Item1).Skip(1).ToList() }).ToList();
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            // Adjust this logic for the new IImageAdapter
            _format.Images = _bitmapInfos.Zip(_format.Images, (b, f) => b.MipMaps.Zip(f, (b2, f2) => (b2, f2.Item2, f2.Item3, f2.Item4, f2.Item5)).ToList()).ToList();
            _format.Save(output.FileData);
        }

        public void Save(Stream output, int versionIndex = 0)
        {
            // Adjust this logic for the new IImageAdapter
            _format.Images = _bitmapInfos.Zip(_format.Images, (b, f) => b.MipMaps.Zip(f, (b2, f2) => (b2, f2.Item2, f2.Item3, f2.Item4, f2.Item5)).ToList()).ToList();
            _format.Save(output);
        }

        public void Dispose() { }
    }
}
