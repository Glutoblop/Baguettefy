namespace Baguettefy.Data.DofusDb.Quests
{

    public partial class QuestItemData
    {
        public string Id { get; set; }
        public bool Cursed { get; set; }
        public bool Usable { get; set; }
        public bool Targetable { get; set; }
        public bool Exchangeable { get; set; }
        public bool TwoHanded { get; set; }
        public bool Etheral { get; set; }
        public long ItemSetId { get; set; }
        public object Criteria { get; set; }
        public string CriteriaTarget { get; set; }
        public bool HideEffects { get; set; }
        public bool Enhanceable { get; set; }
        public bool NonUsableOnAnother { get; set; }
        public long AppearanceId { get; set; }
        public bool SecretRecipe { get; set; }
        public object[] RecipeIds { get; set; }
        public long[] DropMonsterIds { get; set; }
        public object[] DropTemporisMonsterIds { get; set; }
        public bool ObjectIsDisplayOnWeb { get; set; }
        public bool BonusIsSecret { get; set; }
        public object[] PossibleEffects { get; set; }
        public object[] EvolutiveEffectIds { get; set; }
        public object[] FavoriteSubAreas { get; set; }
        public long FavoriteSubAreasBonus { get; set; }
        public long CraftXpRatio { get; set; }
        public bool NeedUseConfirm { get; set; }
        public bool IsDestructible { get; set; }
        public bool IsSaleable { get; set; }
        public double[][] NuggetsBySubarea { get; set; }
        public object[] ContainerIds { get; set; }
        public object[] ResourcesBySubarea { get; set; }
        public bool IsLegendary { get; set; }
        public long QuestItemDataId { get; set; }
        public long NameId { get; set; }
        public Name Name { get; set; }
        public long TypeId { get; set; }
        public long IconId { get; set; }
        public long Level { get; set; }
        public long RealWeight { get; set; }
        public long UseAnimationId { get; set; }
        public long Price { get; set; }
        public long RecipeSlots { get; set; }
        public object CraftVisible { get; set; }
        public object CraftConditional { get; set; }
        public object CraftFeasible { get; set; }
        public object Visibility { get; set; }
        public long ImportantNoticeId { get; set; }
        public ImportantNotice ImportantNotice { get; set; }
        public object ChangeVersion { get; set; }
        public object TooltipExpirationDate { get; set; }
        public object[] Effects { get; set; }
        public Name Slug { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public long V { get; set; }
        public Uri Img { get; set; }
        public Imgset[] Imgset { get; set; }
        public object ItemSet { get; set; }
        public object Appearance { get; set; }
        public TypeClass Type { get; set; }
    }

    public partial class Imgset
    {
        public Uri Url { get; set; }
        public long Size { get; set; }
    }

    public partial class ImportantNotice
    {
        public long Id { get; set; }
    }

    public partial class TypeClass
    {
        public string Id { get; set; }
        public long TypeId { get; set; }
        public long NameId { get; set; }
        public Name Name { get; set; }
        public long SuperTypeId { get; set; }
        public long CategoryId { get; set; }
        public bool IsInEncyclopedia { get; set; }
        public bool Plural { get; set; }
        public long Gender { get; set; }
        public object RawZone { get; set; }
        public bool Mimickable { get; set; }
        public long CraftXpRatio { get; set; }
        public long EvolutiveTypeId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public long V { get; set; }
        public SuperType SuperType { get; set; }
    }

    public partial class SuperType
    {
        public string Id { get; set; }
        public object[] Positions { get; set; }
        public long SuperTypeId { get; set; }
        public Name Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public long V { get; set; }
    }
}
