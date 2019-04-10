using Kontract.Interfaces.Common;
using Kontract.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.Archive;
using Komponent.IO;
using Kontract.Interfaces.Image;
using Kontract;

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
        public IList<FormatInfo> FormatInfos => null;//ImageFormats.CTRFormats.Select(x => new FormatInfo(x.Key, x.Value.FormatName)).ToList();

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
            infos = _format.bitmaps.Select(b => new BitmapInfo(b, new FormatInfo(0x0, ImageFormats.Formats[0x0].FormatName))).ToList();
            //infos = new List<BitmapInfo>() { new BitmapInfo(_format.Texture, new FormatInfo(_format.TextureHeader.Format, ImageFormats.CTRFormats[_format.TextureHeader.Format].FormatName)) { Name = "0" } };
        }

        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            // TODO: Get Kanvas to encode the image and update the UI with it.
            return false;
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            //_format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}