using Newtonsoft.Json;

namespace Baguettefy.Data
{
    public class Item
    {
        [JsonProperty("ankama_id")]
        public long AnkamaId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public TypeClass Type { get; set; }

        [JsonProperty("item_subtype")]
        public string ItemSubtype { get; set; }

        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("image_urls")]
        public ImageUrls ImageUrls { get; set; }
    }

    public class ImageUrls
    {
        [JsonProperty("icon")]
        public Uri Icon { get; set; }

        [JsonProperty("sd")]
        public Uri Sd { get; set; }

        [JsonProperty("hq")]
        public Uri Hq { get; set; }

        [JsonProperty("hd")]
        public Uri Hd { get; set; }
    }

    public class TypeClass
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }
}