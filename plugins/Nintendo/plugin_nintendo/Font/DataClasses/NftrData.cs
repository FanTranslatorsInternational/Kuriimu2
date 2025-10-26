using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace plugin_nintendo.Font.DataClasses
{
    class NftrData
    {
        public List<CharacterInfo> Characters { get; set; }
        public NftrMetaData MetaData { get; set; }
        public NftrImageData ImageData { get; set; }
    }
}
