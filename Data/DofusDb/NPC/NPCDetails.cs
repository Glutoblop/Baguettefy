namespace Baguettefy.Data.DofusDb.NPC
{

    public partial class NpcDetails
    {
        public long Total { get; set; }
        public long Limit { get; set; }
        public long Skip { get; set; }
        public NPCData[] Data { get; set; }
    }

    public partial class NPCData
    {
        public string Id { get; set; }
        public long[][] DialogMessages { get; set; }
        public object[] DialogReplies { get; set; }
        public long[] Actions { get; set; }
        public AnimFunList[] AnimFunList { get; set; }
        public long DatumId { get; set; }
        public long NameId { get; set; }
        public Name Name { get; set; }
        public long Gender { get; set; }
        public string Look { get; set; }
        public bool FastAnimsFun { get; set; }
        public bool TooltipVisible { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public long V { get; set; }
    }

    public partial class AnimFunList
    {
        public long AnimId { get; set; }
        public long EntityId { get; set; }
        public string AnimName { get; set; }
        public long AnimWeight { get; set; }
        public object[] SubAnimFunData { get; set; }
    }

    public partial class Name
    {
        public string De { get; set; }
        public string En { get; set; }
        public string Es { get; set; }
        public string Fr { get; set; }
        public string It { get; set; }
        public string Pt { get; set; }
        public long Id { get; set; }
    }
}
