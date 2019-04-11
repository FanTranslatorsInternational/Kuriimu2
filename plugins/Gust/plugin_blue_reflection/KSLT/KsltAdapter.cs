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

namespace plugin_blue_reflection.KSLT
{
    [Export(typeof(KsltAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("69D27048-0EA2-4C48-A9A3-19521C9115C3", "KSLT (Texture container format)", "KSLT", "Megaflan", "", "This is the KSLT image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.kslt")]
    public sealed class KsltAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private KSLT _format;
        private List<BitmapInfo> infos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => infos;

        [FormFieldIgnore]
        public IList<FormatInfo> FormatInfos => ImageFormats.Formats.Select(x => new FormatInfo(x.Key, x.Value.FormatName)).ToList();

        [FormFieldIgnore]
        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString(8) == "TLSK3100";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new KSLT(input.FileData);
            infos = _format.Bitmaps;
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            // TODO: Get Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}