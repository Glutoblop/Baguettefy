namespace Baguettefy.Data.DofusDb.Dungeons
{

    public partial class AllDungeonData
    {
        public long Total { get; set; }
        public long Limit { get; set; }
        public long Skip { get; set; }
        public DungeonData[] Data { get; set; }
    }

    public partial class DungeonData
    {
        public string Id { get; set; }
        public long DatumId { get; set; }
        public long OptimalPlayerLevel { get; set; }
        public long[] MapIds { get; set; }
        public long EntranceMapId { get; set; }
        public long ExitMapId { get; set; }
        public string ClassName { get; set; }
        public Name Name { get; set; }
        public long MId { get; set; }
        public Name Slug { get; set; }
        public object[] Monsters { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public partial class Name
    {
        public long? Id { get; set; }
        public string De { get; set; }
        public string Es { get; set; }
        public string Pt { get; set; }
        public string Fr { get; set; }
        public string En { get; set; }
    }
}
