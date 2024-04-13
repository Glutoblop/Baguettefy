using Newtonsoft.Json;

namespace Baguettefy.Data.Items
{
    public class ItemData
    {
        [JsonProperty("ankama_id")]
        public long AnkamaId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public ItemType Type { get; set; }

        [JsonProperty("item_subtype")]
        public string ItemSubtype { get; set; }

        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("pods")]
        public long Pods { get; set; }

        [JsonProperty("image_urls")]
        public ImageUrls ImageUrls { get; set; }

        [JsonProperty("effects")]
        public Effect[] Effects { get; set; }

        [JsonProperty("recipe")]
        public Recipe[] Recipe { get; set; }
    }

    public class Effect
    {
        [JsonProperty("int_minimum")]
        public long IntMinimum { get; set; }

        [JsonProperty("int_maximum")]
        public long IntMaximum { get; set; }

        [JsonProperty("type")]
        public EffectType Type { get; set; }

        [JsonProperty("ignore_int_min")]
        public bool IgnoreIntMin { get; set; }

        [JsonProperty("ignore_int_max")]
        public bool IgnoreIntMax { get; set; }

        [JsonProperty("formatted")]
        public string Formatted { get; set; }
    }

    public class EffectType
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_meta")]
        public bool IsMeta { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }
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

    public class Recipe
    {
        [JsonProperty("item_ankama_id")]
        public long ItemAnkamaId { get; set; }

        [JsonProperty("item_subtype")]
        public string ItemSubtype { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }
    }

    public class ItemType
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
