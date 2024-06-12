using Baguettefy.Data.Items;
using Newtonsoft.Json;

namespace Baguettefy.Data.Nuggets
{
    public class NuggetData
    {
        public const int ITEMS_PER_PAGE = 5;

        [JsonProperty("id")]
        public int AnkamaId { get; set; }

        [JsonProperty("recyclingNuggets")]
        public float Ratio { get; set; }

        public static async Task<List<ItemData>> GetNextOrderedItemsAsync(HttpClient client, int nuggetIndex)
        {
            List<ItemData> items = new List<ItemData>();

            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null) return items;
            List<NuggetData> orderedNuggets = nuggetData.OrderByDescending(s => s.Ratio).ToList();
            
            for(; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex++)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                await TestMethod(client, nugget, items);

                if (items.Count >= ITEMS_PER_PAGE)
                {
                    return items;
                }
            }

            return items;
        }

        public static async Task<List<ItemData>> GetPreviousOrderedItemsAsync(HttpClient client, int nuggetIndex)
        {
            List<ItemData> items = new List<ItemData>();

            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null) return items;
            List<NuggetData> orderedNuggets = nuggetData.OrderByDescending(s => s.Ratio).ToList();
            
            for(; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex--)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                await TestMethod(client, nugget, items);

                if (items.Count >= ITEMS_PER_PAGE)
                {
                    return items;
                }
            }

            return items;
        }

        private static async Task TestMethod(HttpClient client, NuggetData nugget, List<ItemData> items)
        {
            var categories = new string[]
            {
                "https://api.dofusdu.de/dofus2/en/items/resources/{0}",
                "https://api.dofusdu.de/dofus2/en/items/consumables/{0}",
            };

            bool found = false;

            foreach (var category in categories)
            {
                var url = string.Format(category, nugget.AnkamaId);

                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                string data = await response.Content.ReadAsStringAsync();
                ItemData? itemData = JsonConvert.DeserializeObject<ItemData>(data);
                if (itemData == null) continue;

                items.Add(itemData);
                found = true;
                break;
            }

            if (!found)
            {
                var url = $"https://api.dofusdu.de/dofus2/en/items/equipment/{nugget.AnkamaId}";
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return;
                string data = await response.Content.ReadAsStringAsync();
                ItemData itemData = ItemData.FromJson(data);
                if (itemData == null) return;

                if (itemData.Recipe == null)
                {
                    items.Add(itemData);
                }
                else
                {
                    List<long> recipeItemIds = itemData.Recipe.Select(s => s.ItemAnkamaId).ToList();   
                }
            }
        }
    }
}
