using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;

namespace plugin_bandai_namco_images.GXT
{
    [Export(typeof(GxtAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("plugin_bandai_namco_images", "GXT Image", "GXT", "IcySon55")]
    [PluginExtensionInfo("*.gxt;*.bin")]
    public class GxtAdapter : IImageAdapter, /*IIndexedImageAdapter,*/ IIdentifyFiles, ILoadFiles/*, ISaveFiles*/
    {
        private GXT _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public IList<FormatInfo> FormatInfos => GXT.Formats.Select(x => new FormatInfo((int)x.Key, x.Value.FormatName)).Union(GXT.PaletteFormats.Select(x => new FormatInfo((int)x.Key, x.Value.FormatName))).ToList();

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString(4) == "GXT\0";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new GXT(input.FileData);
            _bitmapInfos = new List<BitmapInfo> { new BitmapInfo(_format.Texture, new FormatInfo((int)0, _format.FormatName)) };
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {





            return false;
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
