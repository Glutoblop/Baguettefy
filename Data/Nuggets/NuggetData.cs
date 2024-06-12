using Newtonsoft.Json;

namespace Baguettefy.Data.Nuggets
{
    public class NuggetData
    {
        [JsonProperty("id")]
        public int AnkamaId { get; set; }

        [JsonProperty("recyclingNuggets")]
        public float Amount { get; set; }
    }
}
