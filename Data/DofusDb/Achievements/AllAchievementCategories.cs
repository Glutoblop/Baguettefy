using Newtonsoft.Json;

namespace Baguettefy.Data.DofusDb.Achievements
{
    public class AllAchievementCategories
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("skip")]
        public long Skip { get; set; }

        [JsonProperty("data")]
        public List<AchievementCategory> Data { get; set; }
    }

    public class AchievementCategory
    {
        [JsonProperty("achievementIds")]
        public object[] AchievementIds { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("nameId")]
        public long NameId { get; set; }

        [JsonProperty("name")]
        public Name Name { get; set; }

        [JsonProperty("parentId")]
        public long ParentId { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("order")]
        public long Order { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("visibilityCriterion")]
        public object VisibilityCriterion { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("_include")]
        public string[] Include { get; set; }

        [JsonProperty("parent")]
        public object Parent { get; set; }
    }
}
