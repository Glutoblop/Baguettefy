using Baguettefy.Core.Interfaces;
using Baguettefy.Data;
using Baguettefy.Data.Quests;
using Newtonsoft.Json;

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

            //Categories contain types of quests, loop through these
            for (int category = 2; category < 100; category++)
            {
                var pageMissingCount = 0;
                Console.WriteLine($"#### Search Category {category}...");
                await Task.Delay(100);

                //Loop through the pages in this category
                for (int page = 0; page < 10; page++)
                {

                    var url = $"https://api.dofusdb.fr/quests?$";
                    var skipStartIndex = page * 50;
                    var allQuestsUrl = $"{url}skip={skipStartIndex}&$populate=true&$limit=50&categoryId={category}&lang=en";
                    //allQuestsUrl = $"https://api.dofusdb.fr/quests?$skip=0&$populate=true&$limit=1&categoryId=4&lang=en";

                    HttpResponseMessage response = await client.GetAsync(allQuestsUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        AllQuestsData? allQuests = JsonConvert.DeserializeObject<AllQuestsData>(json);

                        if (allQuests?.Quests?.Length > 0)
                        {
                            pageMissingCount = 0;

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
                            Console.WriteLine($"No data found for category:{category} page:{page}");
                            pageMissingCount++;
                            if (pageMissingCount > 2)
                            {
                                Console.WriteLine($"Assuming end of Category, skipping to next.");
                                break;
                            }
                        }
                    }

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
    }
}
