using Baguettefy.Core.Interfaces;
using Baguettefy.Data;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.DofusDb.Dungeons;
using Baguettefy.Data.DofusDb.Quests;
using Baguettefy.Data.Quests;
using Newtonsoft.Json;

namespace Baguettefy.Cache
{
    public class UpdateDatabase
    {
        static HttpClient client = new HttpClient();

        public static async Task Update(IFirebaseDatabase db, bool force = false)
        {
            DateTime now = DateTime.UtcNow;

            if (!force)
            {
                var completeData = await db.GetAsync<CacheComplete>($"Completed/QuestsGrabbed");
                if (completeData?.IsComplete ?? false)
                {
                    var duration = now - completeData.TimeStamp;
                    if (duration < TimeSpan.FromDays(30)) return;
                }
            }

            //await FetchAchievementCategoriesData(db);
            //await FetchAchievementData(db);
            //
            //await FetchQuestCategoriesData(db);
            //await FetchQuestData(db);

            await FetchDungeonData(db);

            Console.WriteLine($"Completed in :{DateTime.UtcNow - now:g}");

            await db.PutAsync($"Completed/QuestsGrabbed", new CacheComplete()
            {
                IsComplete = true,
                TimeStamp = DateTime.UtcNow
            });

        }

        private static async Task FetchQuestCategoriesData(IFirebaseDatabase db)
        {
            AllQuestCategories allQuestCategories = null;

            for (int i = 0; i < 5; i++)
            {
                var pageSize = 50;
                var startIndex = i * pageSize;
                var url = $"https://api.dofusdb.fr/quest-categories?$";
                var questCategoriesUrl = $"{url}skip={startIndex}&$limit={pageSize}&$sort=order&lang=en";

                HttpResponseMessage response = await client.GetAsync(questCategoriesUrl);
                if (!response.IsSuccessStatusCode) continue;

                string json = await response.Content.ReadAsStringAsync();
                AllQuestCategories? allCategories = JsonConvert.DeserializeObject<AllQuestCategories>(json);

                if (allCategories?.Data?.Count <= 0) continue;
                if (allQuestCategories == null) allQuestCategories = allCategories;
                else
                {
                    if (allQuestCategories.Data == null) allQuestCategories = new AllQuestCategories();
                    allCategories.Data.AddRange(allCategories.Data);
                }
            }

            await db.PutAsync($"QuestCategories", allQuestCategories);
        }

        private static async Task FetchQuestData(IFirebaseDatabase db)
        {
            var questCategories = await db.GetAsync<AllQuestCategories>($"QuestCategories");
            foreach (var questCategory in questCategories.Data)
            {
                var pageCount = Math.Ceiling(questCategory.QuestIds.Length / 50.0f);
                for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    var pageSize = 50;
                    var startIndex = pageIndex * pageSize;
                    var url = $"https://api.dofusdb.fr/quests?$";
                    var allQuestsUrl = $"{url}skip={startIndex}&$populate=true&$limit={pageSize}&categoryId={questCategory.Id}&lang=en";

                    HttpResponseMessage response = await client.GetAsync(allQuestsUrl);
                    if (!response.IsSuccessStatusCode) continue;

                    string json = await response.Content.ReadAsStringAsync();
                    AllQuestsData? allQuests = JsonConvert.DeserializeObject<AllQuestsData>(json);

                    if (allQuests?.Quests?.Length > 0)
                    {
                        foreach (var quest in allQuests?.Quests)
                        {
                            var data = await db.GetAsync<QuestData>($"Quest/{quest.Id}");
                            if (data == null)
                            {
                                await db.PutAsync($"Quest/{quest.Id}", quest);

                                Console.WriteLine($"{quest.Name.En} added to cached [{quest.Id}]");
                            }
                            else
                            {
                                Console.WriteLine($"{quest.Name.En} already cached [{quest.Id}]");
                            }
                        }
                    }

                    await Task.Delay(350);
                }
            }
        }

        private static async Task FetchAchievementCategoriesData(IFirebaseDatabase db)
        {
            AllAchievementCategories allAchievementCategories = null;
            bool validResponse = true;
            int i = 0;
            while (validResponse)
            {
                var pageSize = 50;
                var startIndex = i++ * pageSize;
                var url = $"https://api.dofusdb.fr/achievement-categories?$";
                var questCategoriesUrl = $"{url}skip={startIndex}&$limit={pageSize}&$sort=order&lang=en";

                HttpResponseMessage response = await client.GetAsync(questCategoriesUrl);
                if (!response.IsSuccessStatusCode) continue;

                string json = await response.Content.ReadAsStringAsync();
                AllAchievementCategories? allCategories = JsonConvert.DeserializeObject<AllAchievementCategories>(json);

                if (allCategories?.Data?.Count <= 0)
                {
                    validResponse = false;
                    continue;
                }
                if (allAchievementCategories == null) allAchievementCategories = allCategories;
                else
                {
                    if (allAchievementCategories.Data == null) allAchievementCategories = new AllAchievementCategories();
                    allAchievementCategories.Data.AddRange(allCategories.Data);
                }
            }

            await db.PutAsync($"AchievementCategories", allAchievementCategories);
        }

        private static async Task FetchAchievementData(IFirebaseDatabase db)
        {
            var achievementCategories = await db.GetAsync<AllAchievementCategories>($"AchievementCategories");
            foreach (var categoryData in achievementCategories.Data)
            {
                var pageCount = Math.Ceiling(categoryData.AchievementIds.Length / 50.0f);
                for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    var pageSize = 50;
                    var startIndex = pageIndex * pageSize;
                    var url = $"https://api.dofusdb.fr/achievements?$";
                    var allAchievementUrl = $"{url}skip={startIndex}&$populate=true&$limit={pageSize}&categoryId={categoryData.Id}&lang=en";

                    HttpResponseMessage achievementResponse = await client.GetAsync(allAchievementUrl);
                    if (!achievementResponse.IsSuccessStatusCode) continue;

                    string json = await achievementResponse.Content.ReadAsStringAsync();
                    AllAchievementData? allAchievements = JsonConvert.DeserializeObject<AllAchievementData>(json);

                    if (allAchievements?.Achievements?.Length > 0)
                    {
                        foreach (var achievement in allAchievements?.Achievements)
                        {
                            var cachedAchievementData = await db.GetAsync<AchievementData>($"Achievement/{achievement.Id}");
                            if (cachedAchievementData == null)
                            {
                                Console.WriteLine($"{achievement.Name.En} added to cached [{achievement.Id}]");
                            }
                            else
                            {
                                Console.WriteLine($"{achievement.Name.En} already cached [{achievement.Id}]");

                                if (cachedAchievementData.Objectives?.Count == achievement.ObjectiveIds.Count)
                                {
                                    continue;
                                }
                            }

                            var objectiveIds = achievement.ObjectiveIds.Aggregate($"", (current, id) => current + $"&id[]={id}");

                            var objectiveUrl = $"https://api.dofusdb.fr/achievement-objectives?{objectiveIds}";
                            HttpResponseMessage objectiveResponse = await client.GetAsync(objectiveUrl);
                            if (!achievementResponse.IsSuccessStatusCode) continue;

                            string objJson = await objectiveResponse.Content.ReadAsStringAsync();
                            AllAchievementObjectiveData? allObjectives = JsonConvert.DeserializeObject<AllAchievementObjectiveData>(objJson);
                            achievement.Objectives = allObjectives?.Data ?? new List<AchievementObjective>();

                            await db.PutAsync($"Achievement/{achievement.Id}", achievement);

                            await Task.Delay(350);
                        }
                    }

                    await Task.Delay(350);
                }
            }
        }

        private static async Task FetchDungeonData(IFirebaseDatabase db)
        {
            bool endOfList = false;

            int pageIndex = 0;
            var pageSize = 50;
            

            while (!endOfList)
            {
                var startIndex = pageIndex * pageSize;
                var url = $"https://api.dofusdb.fr/dungeons?$";
                var dungeonURL = $"{url}skip={startIndex}&$sort=-1&$populate=true&$limit={pageSize}&lang=en";

                pageIndex++;

                await Task.Delay(350);
                HttpResponseMessage achievementResponse = await client.GetAsync(dungeonURL);
                if (!achievementResponse.IsSuccessStatusCode) continue;

                string json = await achievementResponse.Content.ReadAsStringAsync();
                AllDungeonData? allDungeons = JsonConvert.DeserializeObject<AllDungeonData>(json);

                if (allDungeons?.Data?.Length > 0)
                {
                    foreach (var dungeonData in allDungeons?.Data)
                    {
                        var cachedDungeonData = await db.GetAsync<DungeonData>($"Dungeon/{dungeonData.Id}");

                        if (cachedDungeonData == null)
                        {
                            Console.WriteLine($"{dungeonData.Name.En} added to cached [{dungeonData.Id}]");
                            await db.PutAsync($"Dungeon/{dungeonData.Id}", dungeonData);
                        }
                        else
                        {
                            Console.WriteLine($"{dungeonData.Name.En} already cached [{dungeonData.Id}]");
                        }
                    }
                }
                else
                {
                    endOfList = true;
                }
            }
        }
    }
}
