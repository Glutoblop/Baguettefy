using Newtonsoft.Json;

namespace Baguettefy.Data.DofusDb.Achievements
{
    public partial class AllAchievementData
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("skip")]
        public long Skip { get; set; }

        [JsonProperty("data")]
        public AchievementData[] Achievements { get; set; }
    }

    public partial class AchievementData
    {
        [JsonProperty("objectiveIds")]
        public List<long> ObjectiveIds { get; set; }

        [JsonProperty("rewardIds")]
        public List<long> RewardIds { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("categoryId")]
        public long CategoryId { get; set; }

        [JsonProperty("NameId")]
        public long NameId { get; set; }

        [JsonProperty("Name")]
        public Name Name { get; set; }

        [JsonProperty("iconId")]
        public long IconId { get; set; }

        [JsonProperty("points")]
        public long Points { get; set; }

        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("order")]
        public long Order { get; set; }

        [JsonProperty("accountLinked")]
        public bool AccountLinked { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("slug")]
        public Name Slug { get; set; }

        [JsonProperty("img")]
        public Uri Img { get; set; }

        [JsonProperty("objectives")]
        public List<AchievementObjective> Objectives { get; set; }

        [JsonProperty("category")]
        public Category Category { get; set; }

        [JsonProperty("rewards")]
        public List<Reward> Rewards { get; set; }
    }

    public partial class Category
    {
        [JsonProperty("achievementIds")]
        public List<long> AchievementIds { get; set; }

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
    }

    public partial class Reward
    {
        [JsonProperty("itemsReward")]
        public List<object> ItemsReward { get; set; }

        [JsonProperty("itemsQuantityReward")]
        public List<object> ItemsQuantityReward { get; set; }

        [JsonProperty("emotesReward")]
        public List<object> EmotesReward { get; set; }

        [JsonProperty("spellsReward")]
        public List<object> SpellsReward { get; set; }

        [JsonProperty("titlesReward")]
        public List<object> TitlesReward { get; set; }

        [JsonProperty("ornamentsReward")]
        public List<object> OrnamentsReward { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("achievementId")]
        public long AchievementId { get; set; }

        [JsonProperty("criteria")]
        public object Criteria { get; set; }

        [JsonProperty("kamasRatio")]
        public double KamasRatio { get; set; }

        [JsonProperty("experienceRatio")]
        public long ExperienceRatio { get; set; }

        [JsonProperty("kamasScaleWithPlayerLevel")]
        public bool KamasScaleWithPlayerLevel { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("items")]
        public List<object> Items { get; set; }

        [JsonProperty("ornaments")]
        public List<object> Ornaments { get; set; }

        [JsonProperty("titles")]
        public List<object> Titles { get; set; }
    }
}
