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

        public static async Task<List<Tuple<ItemData, NuggetData>>> GetNextOrderedItemsAsync(HttpClient client, List<NuggetData> nuggetData, int nuggetIndex)
        {
            List<Tuple<ItemData, NuggetData>> items = new List<Tuple<ItemData, NuggetData>>();

            var categories = new string[]
            {
                "https://api.dofusdu.de/dofus2/en/items/resources/{0}",
                "https://api.dofusdu.de/dofus2/en/items/consumables/{0}",
                "https://api.dofusdu.de/dofus2/en/items/equipment/{0}"
            };

            List<NuggetData> orderedNuggets = nuggetData.OrderByDescending(s => s.Ratio).ToList();
            
            for(; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex++)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                foreach (var category in categories)
                {
                    var url = string.Format(category, nugget.AnkamaId);

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) continue;

                    string data = await response.Content.ReadAsStringAsync();
                    ItemData? itemData = JsonConvert.DeserializeObject<ItemData>(data);
                    if (itemData == null) continue;

                    items.Add(new Tuple<ItemData, NuggetData>(itemData, nugget));
                    break;
                }

                if (items.Count >= ITEMS_PER_PAGE)
                {
                    return items;
                }
            }

            return items;
        }

        public static async Task<List<Tuple<ItemData, NuggetData>>> GetPreviousOrderedItemsAsync(HttpClient client, List<NuggetData> nuggetData, int startingIndex)
        {
            List<Tuple<ItemData, NuggetData>> items = new List<Tuple<ItemData, NuggetData>>();

            var categories = new string[]
            {
                "https://api.dofusdu.de/dofus2/en/items/resources/{0}",
                "https://api.dofusdu.de/dofus2/en/items/consumables/{0}",
                "https://api.dofusdu.de/dofus2/en/items/equipment/{0}"
            };
            
            List<NuggetData> orderedNuggets = nuggetData.OrderByDescending(s => s.Ratio).ToList();
            
            for(; startingIndex < orderedNuggets.Count && startingIndex >= 0; startingIndex--)
            {
                NuggetData nugget = orderedNuggets[startingIndex];

                foreach (var category in categories)
                {
                    var searchUrl = string.Format(category, nugget.AnkamaId);

                    HttpResponseMessage response = await client.GetAsync(searchUrl);
                    if (!response.IsSuccessStatusCode) continue;

                    string data = await response.Content.ReadAsStringAsync();
                    ItemData? itemData = JsonConvert.DeserializeObject<ItemData>(data);
                    if (itemData == null) continue;

                    items.Add(new Tuple<ItemData, NuggetData>(itemData, nugget));
                    break;
                }

                if (items.Count >= ITEMS_PER_PAGE)
                {
                    break;
                }
            }

            items = items.OrderByDescending(s => s.Item2.Ratio).ToList();

            return items;
        }
    }
}
