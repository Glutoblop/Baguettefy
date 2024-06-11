using Newtonsoft.Json;

namespace Baguettefy.Data
{
    public class NuggetData
    {
        [JsonProperty("id")]
        public int AnkamaId { get; set; }

        [JsonProperty("recyclingNuggets")]
        public float Ratio { get; set; }
    }
}
