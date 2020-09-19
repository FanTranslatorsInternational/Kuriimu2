using System;
using System.Collections.Generic;
using System.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using Kontract.Models;
using Kontract.Models.Image;

// TODO: Add all image features

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

        public Task<ImageTranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding)
        {
            throw new NotImplementedException();
        }

        [FormFieldIgnore]
        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

        public IList<EncodingInfo> ImageEncodingInfos => (TEX.Version)(_format?.HeaderInfo.Format ?? 0xa4) == TEX.Version._Switchv1 ? TEX.SwitchEncodingInfos : TEX.EncodingInfos;

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input, BaseReadOnlyDirectoryNode fs)
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

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode fs)
        {
            _format = new TEX(input.FileData);
            // TODO: Implement support for properly populating the FormatInfo for MTTEX.
            _bitmapInfos = _format.Bitmaps;
        }

        public Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo formatInfo, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }

        public void Save(StreamInfo output, PhysicalDirectoryNode fs, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
