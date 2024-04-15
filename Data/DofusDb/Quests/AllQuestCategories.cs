using Newtonsoft.Json;

namespace Baguettefy.Data.DofusDb.Quests
{

    public class AllQuestCategories
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("skip")]
        public long Skip { get; set; }

        [JsonProperty("data")]
        public List<QuestCategory> Data { get; set; }
    }

    public class QuestCategory
    {
        [JsonProperty("questIds")]
        public long[] QuestIds { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("nameId")]
        public long NameId { get; set; }

        [JsonProperty("name")]
        public Name Name { get; set; }

        [JsonProperty("order")]
        public long Order { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}