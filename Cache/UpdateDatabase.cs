using Baguettefy.Core.Interfaces;
using Baguettefy.Data;
using Baguettefy.Data.DofusDb.Quests;
using Baguettefy.Data.Quests;
using Google.Api.Gax;
using Newtonsoft.Json;
using System;

namespace Baguettefy.Cache
{
    public class UpdateDatabase
    {
        static HttpClient client = new HttpClient();

        public async Task Update(IFirebaseDatabase db, bool force = false)
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
            
            await FetchQuestCategoriesData(db);

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

                    await FetchQuestData(db, allQuestsUrl);

                    await Task.Delay(350);
                }
            }

            Console.WriteLine($"Completed in :{DateTime.UtcNow - now:g}");

            await db.PutAsync($"Completed/QuestsGrabbed", new CacheComplete()
            {
                IsComplete = true,
                TimeStamp = DateTime.UtcNow
            });

        }

        private async Task FetchQuestCategoriesData(IFirebaseDatabase db)
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
                    if(allQuestCategories.Data == null) allQuestCategories = new AllQuestCategories();
                    allCategories.Data.AddRange(allCategories.Data);
                }
            }

            await db.PutAsync($"QuestCategories", allQuestCategories);
        }

        private async Task<bool> FetchQuestData(IFirebaseDatabase db, string allQuestsUrl)
        {
            HttpResponseMessage response = await client.GetAsync(allQuestsUrl);
            if (!response.IsSuccessStatusCode) return false;

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
            else
            {
                return true;
            }

            return false;
        }
    }
}
