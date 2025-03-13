using Baguettefy.Core.Interfaces;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.DofusDb.Dungeons;
using Baguettefy.Data.Quests;
using Discord;

namespace Baguettefy
{
    public class FindTranslationData
    {
        public static async Task<EmbedBuilder> FindQuest(IFirebaseDatabase db, string name)
        {
            QuestData? foundQuest = null;
            await db.GetAllAsync<QuestData>($"Quest", (path, item) =>
            {
                if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundQuest = item;
                    return true;
                }

                if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundQuest = item;
                    return true;
                }
                return false;
            });

            if (foundQuest == null) return null;
            return EmbedCreator.CreateTranslatedEmbed("Quest", foundQuest.Name.En, foundQuest.Name.Fr);
        }

        public static async Task<EmbedBuilder> FindAchievement(IFirebaseDatabase db, string name)
        {
            AchievementData? foundAchievement = null;
            await db.GetAllAsync<AchievementData>($"Achievement", (path, item) =>
            {
                if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundAchievement = item;
                    return true;
                }

                if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundAchievement = item;
                    return true;
                }

                return false;
            });
            if (foundAchievement == null) return null;
            return EmbedCreator.CreateTranslatedEmbed("Achievement", foundAchievement.Name.En, foundAchievement.Name.Fr);
        }

        internal static async Task<EmbedBuilder> FindDungeon(IFirebaseDatabase db, string name)
        {
            DungeonData? foundDungeon = null;
            await db.GetAllAsync<DungeonData>($"Dungeon", (path, item) =>
            {
                if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundDungeon = item;
                    return true;
                }

                if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundDungeon = item;
                    return true;
                }

                return false;
            });
            if (foundDungeon == null) return null;
            return EmbedCreator.CreateTranslatedEmbed("Dungeon", foundDungeon.Name.En, foundDungeon.Name.Fr);
        }
    }
}
