using Newtonsoft.Json;

namespace Kore.Models.Update
{
    public class Manifest
    {
        [JsonProperty("source_type")]
        public string SourceType { get; set; }

        [JsonProperty("build_number")]
        public string BuildNumber { get; set; }

        [JsonProperty("application_name")]
        public string ApplicationName { get; set; }
    }
}
