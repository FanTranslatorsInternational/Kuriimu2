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

namespace plugin_mt_framework.TEX
{
    [Export(typeof(TexAdapter))]
    [Export(typeof(IPlugin))]
    [Export(typeof(IMtFrameworkTextureAdapter))]
    [PluginInfo("plugin_mt_framework_TEX", "MT Framework Texture", "TEX", "IcySon55", "", "This is the TEX image adapter for Kuriimu.")]
    [PluginExtensionInfo("*.tex")]
    public sealed class TexAdapter : IImageAdapter, IIdentifyFiles, ICreateFiles, ILoadFiles, ISaveFiles, IMtFrameworkTextureAdapter
    {
        private TEX _format;
        private List<BitmapInfo> _bitmapInfos;

        #region Properties

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public bool LeaveOpen { get; set; }

        public IList<FormatInfo> FormatInfos => null;

        #endregion

        public bool Identify(StreamInfo input)
        {
            var result = true;

            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                {
                    var magic = br.ReadString(4);
                    if (magic != "TEX\0" && magic != "\0XET")
                        result = false;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void Create()
        {
            //_format = new MTTEX();
        }

        public void Load(StreamInfo input)
        {
            _format = new TEX(input.FileData);
            // TODO: Implement support for properly populating the FormatInfo for MTTEX.
            _bitmapInfos = new List<BitmapInfo> { new BitmapInfo(_format.Bitmaps.First(), new FormatInfo(0, "This doesn't work.") ) { Name = "0", MipMaps = _format.Bitmaps.Skip(1).ToList() } };
        }

        public Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
