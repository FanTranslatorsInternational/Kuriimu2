//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;
//using System.Threading.Tasks;
//using Komponent.IO;
//using Kontract;
//using Kontract.Attributes;
//using Kontract.Interfaces.Common;
//using Kontract.Interfaces.Image;
//using plugin_nintendo.NW4C;

//namespace plugin_nintendo.BCLIM
//{
//    [Export(typeof(BclimAdapter))]
//    [Export(typeof(IPlugin))]
//    [PluginInfo("FAD19315-1A30-44A3-B0D4-0E6A8E71A39F", "NW4C BCLIM Image", "BCLIM", "IcySon55", "", "This is the BCLIM image adapter for Kuriimu.")]
//    [PluginExtensionInfo("*.bclim")]
//    public sealed class BclimAdapter : IImageAdapter, IIdentifyFiles, /*ICreateFiles,*/ ILoadFiles, ISaveFiles
//    {
//        private BCLIM _format;

//        private List<BitmapInfo> _bitmapInfos;

//        #region Properties

//        [FormFieldIgnore]
//        public IList<BitmapInfo> BitmapInfos => _bitmapInfos;

//        [FormFieldIgnore]
//        public IList<FormatInfo> FormatInfos => ImageFormats.CtrFormats.Select(x => new FormatInfo(x.Key, x.Value.FormatName)).ToList();

//        [FormFieldIgnore]
//        public bool LeaveOpen { get; set; }

//        #endregion

//        public bool Identify(StreamInfo input)
//        {
//            var result = true;

//            try
//            {
//                using (var br = new BinaryReaderX(input.FileData, true))
//                {
//                    if (br.BaseStream.Length < 0x28) return false;

//                    br.BaseStream.Position = br.BaseStream.Length - 0x28;
//                    if (br.ReadString(4) != "CLIM")
//                        result = false;
//                }
//            }
//            catch (Exception)
//            {
//                result = false;
//            }

//            return result;
//        }

//        //public void Create()
//        //{
//        //    _format = new BCLIM();
//        //}

//        public void Load(StreamInfo input)
//        {
//            _format = new BCLIM(input.FileData);
//            _bitmapInfos = new List<BitmapInfo>() { new BitmapInfo(_format.Texture, new FormatInfo(_format.TextureHeader.Format, ImageFormats.CtrFormats[_format.TextureHeader.Format].FormatName)) { Name = "0" } };
//        }

//        public async Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress)
//        {
//            // TODO: Get Kanvas to encode the image and update the UI with it.
//            return false;
//        }

//        public void Save(StreamInfo output, int versionIndex = 0)
//        {
//            _format.Save(output.FileData);
//        }

//        public void Dispose() { }
//    }
//}
