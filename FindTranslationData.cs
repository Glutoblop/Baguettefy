using Baguettefy.Core.Interfaces;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.DofusDb.Dungeons;
using Baguettefy.Data.Items;
using Baguettefy.Data.Quests;
using Discord;
using Newtonsoft.Json;

namespace Baguettefy
{
    public class FindTranslationData
    {
        static readonly HttpClient _Client = new HttpClient();

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

        internal static async Task<EmbedBuilder> FindItem(string name)
        {
            string[] searches = new string[]
                {
                    $"https://api.dofusdu.de/dofus2/fr/items/search?query={name}&limit=1",
                    $"https://api.dofusdu.de/dofus2/en/items/search?query={name}&limit=1"
                };

            ItemData? searchData = null;
            HttpResponseMessage response = null;

            for (int i = 0; i < searches.Length; i++)
            {
                response = await _Client.GetAsync(searches[i]);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    searchData = JsonConvert.DeserializeObject<ItemData[]>(data)?.FirstOrDefault();
                }
            }

            if (searchData == null)
            {
                return null;
            }

            var itemType = searchData.ItemSubtype switch
            {
                "consumables" => "items/consumables",
                "cosmetics" => "items/cosmetics",
                "resources" => "items/resources",
                "equipment" => "items/equipment",
                "quest" => "items/quest",
                "mounts" => "mounts",
                "sets" => "sets",
                _ => ""
            };

            var english_detail_url = $"https://api.dofusdu.de/dofus2/en/{itemType}/{searchData.AnkamaId}";
            ItemData? english_item = null;
            HttpResponseMessage english_detail_response = await _Client.GetAsync(english_detail_url);
            if (english_detail_response.IsSuccessStatusCode)
            {
                string data = await english_detail_response.Content.ReadAsStringAsync();
                english_item = JsonConvert.DeserializeObject<ItemData>(data);
            }

            if (english_item == null)
            {
                return null;
            }

            var french_detail_url = $"https://api.dofusdu.de/dofus2/fr/{itemType}/{searchData.AnkamaId}";
            ItemData? french_item = null;
            HttpResponseMessage french_detail_response = await _Client.GetAsync(french_detail_url);
            if (french_detail_response.IsSuccessStatusCode)
            {
                string data = await french_detail_response.Content.ReadAsStringAsync();
                french_item = JsonConvert.DeserializeObject<ItemData>(data);
            }

            if (french_item == null)
            {
                return null;
            }

            EmbedBuilder embedBuilder = EmbedCreator.CreateTranslatedEmbed($"", english_item.Name, french_item.Name);
            embedBuilder.Title = $"Search: '{name}' found..";
            embedBuilder.ThumbnailUrl = english_item.ImageUrls.Icon?.AbsoluteUri;
            embedBuilder.AddField(
                new EmbedFieldBuilder()
                .WithName($"Category")
                .WithValue($"{(english_item?.Type?.Name ?? "Unknown")}")
                .WithIsInline(false)
            );
            embedBuilder.AddField(
                new EmbedFieldBuilder()
                .WithName($"Description")
                .WithValue($"{(english_item?.Description ?? "Unknown")}")
                .WithIsInline(false)
            );
            return embedBuilder;
        }
    }
}
