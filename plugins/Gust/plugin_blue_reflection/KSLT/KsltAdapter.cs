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

namespace plugin_blue_reflection.KSLT
{
    [Export(typeof(KsltAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("69D27048-0EA2-4C48-A9A3-19521C9115C3", "KSLT (Texture container format)", "KSLT", "Megaflan", "", "This is the KSLT image adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.kslt")]
    public sealed class KsltAdapter : IImageAdapter, IIdentifyFiles, ILoadFiles, ILoadStreams, ISaveFiles, ISaveStreams
    {
        private KSLT _format;

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
        }

        public void Dispose() { }
    }
}