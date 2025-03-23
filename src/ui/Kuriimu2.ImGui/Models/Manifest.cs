using System.Text.Json.Serialization;

namespace Kuriimu2.ImGui.Models
{
    internal class Manifest
    {
        [JsonPropertyName("source_type")]
        public string SourceType { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("build_number")]
        public string BuildNumber { get; set; }
    }
}
