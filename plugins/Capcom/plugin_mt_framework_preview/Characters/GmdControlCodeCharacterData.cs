using Kaligraphy.DataClasses.Parsing;

namespace plugin_mt_framework_preview.Characters
{
    class GmdControlCodeCharacterData : ControlCodeCharacterData
    {
        public required string Code { get; init; }

        public required IReadOnlyList<string> Arguments { get; init; }
    }
}
