using System.Text.Json.Serialization;

namespace Kuriimu2.Cmd.Models
{
    public class Manifest
    {
        [JsonPropertyName("source_type")]
        public required string SourceType { get; set; }

        [JsonPropertyName("version")]
        public required string Version { get; set; }

        [JsonPropertyName("build_number")]
        public required string BuildNumber { get; set; }
    }

    [JsonSerializable(typeof(Manifest))]
    public partial class ManifestJsonSerializerContext : JsonSerializerContext;
}
