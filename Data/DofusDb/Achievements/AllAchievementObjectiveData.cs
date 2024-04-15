using Newtonsoft.Json;

namespace Baguettefy.Data.DofusDb.Achievements
{
    public partial class AllAchievementObjectiveData
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("skip")]
        public long Skip { get; set; }

        [JsonProperty("data")]
        public List<AchievementObjective> Data { get; set; }
    }

    public partial class AchievementObjective
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("achievementId")]
        public long AchievementId { get; set; }

        [JsonProperty("order")]
        public long Order { get; set; }

        [JsonProperty("nameId")]
        public long NameId { get; set; }

        [JsonProperty("name")]
        public Name Name { get; set; }

        [JsonProperty("criterion")]
        public string Criterion { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
