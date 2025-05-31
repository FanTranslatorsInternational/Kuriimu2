using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace plugin_nintendo.Font.DataClasses
{
    class CfntData
    {
        public List<CharacterInfo> Characters { get; set; }
        public CfntMetaData MetaData { get; set; }
        public CfntImageData ImageData { get; set; }
    }
}
