using Baguettefy.Data.Items;
using Discord;
using Newtonsoft.Json;

namespace Baguettefy.Data.Nuggets
{
    public class NuggetUtils
    {
        public const int ITEMS_PER_PAGE = 5;

        public static async Task<List<ItemData>> GetNextOrderedItemsAsync(HttpClient client, int nuggetIndex)
        {
            List<ItemData> items = new List<ItemData>();

            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null) return items;
            List<NuggetData> orderedNuggets = nuggetData.OrderByDescending(s => s.Amount).ToList();

            for (; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex++)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                var item = await GetItem(client, nugget);
                if(item != null) items.Add(item);

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
            List<NuggetData> orderedNuggets = nuggetData.OrderByDescending(s => s.Amount).ToList();

            for (; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex--)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                var item = await GetItem(client, nugget);
                if(item != null) items.Add(item);

                if (items.Count >= ITEMS_PER_PAGE)
                {
                    return items;
                }
            }

            return items;
        }

        private static async Task<ItemData?> GetItem(HttpClient client, NuggetData nugget)
        {
            var categories = new string[]
            {
                "https://api.dofusdu.de/dofus2/en/items/resources/{0}",
                "https://api.dofusdu.de/dofus2/en/items/consumables/{0}",
                "https://api.dofusdu.de/dofus2/en/items/equipment/{0}",
            };
            
            foreach (var category in categories)
            {
                var url = string.Format(category, nugget.AnkamaId);

                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                string data = await response.Content.ReadAsStringAsync();
                ItemData? itemData = ItemData.FromJson(data);
                if (itemData == null) continue;

                return itemData;
            }

            return null;
        }

        public static async Task<float> GetNuggetValue(HttpClient client, long ankamaId)
        {
            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null) return 0;

            var categories = new string[]
            {
                "https://api.dofusdu.de/dofus2/en/items/resources/{0}",
                "https://api.dofusdu.de/dofus2/en/items/consumables/{0}",
                "https://api.dofusdu.de/dofus2/en/items/equipment/{0}",
            };
            
            foreach (var category in categories)
            {
                var url = string.Format(category, ankamaId);

                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                string data = await response.Content.ReadAsStringAsync();
                ItemData? itemData = ItemData.FromJson(data);
                if (itemData == null) continue;

                if (itemData.Recipe == null)
                {
                    var nugget = nuggetData.FirstOrDefault(s => s.AnkamaId == itemData.AnkamaId);
                    if (nugget == null) return 0;
                    return nugget.Amount;
                }
                else
                {
                    float nuggetValue = 0;
                    foreach (var recipeItem in itemData.Recipe)
                    {
                        var val = await GetNuggetValue(client, recipeItem.ItemAnkamaId);
                        nuggetValue += (val * 1.5f) * recipeItem.Quantity;
                    }
                    return nuggetValue;
                }
            }

            return 0;
        }
    }
}
