using Baguettefy.Data.Items;
using Discord;
using Newtonsoft.Json;
using System.Globalization;
using System.IO.Compression;

namespace Baguettefy.Data.Nuggets
{
    public class NuggetUtils
    {
        public const int ITEMS_PER_PAGE = 5;

        private static List<NuggetData> _NuggetData = new List<NuggetData>();

        public const string zipFileSource = "res/CachedNuggets.zip";
        public const string zipFileDestination = $"CachedNuggets";

        public static async Task Init()
        {
            if (Directory.Exists(zipFileDestination))
            {
                Directory.Delete(zipFileDestination,true);          
            }

            if (!Directory.Exists(zipFileDestination))
            {
                ZipFile.ExtractToDirectory(zipFileSource, zipFileDestination);
            }

            using HttpClient client = new HttpClient();
            await GetNuggetData(client);
        }

        public static async Task<List<NuggetData>?> GetNuggetData(HttpClient client, bool ignoreGetCache = false)
        {
            if (_NuggetData?.Count != 0) return _NuggetData;
            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null) return new List<NuggetData>();
            _NuggetData = nuggetData.OrderByDescending(s => GetNuggetValue(client, s.AnkamaId, ignoreGetCache).GetAwaiter().GetResult()).ToList();
            return _NuggetData;
        }

        public static async Task<List<ItemData>> GetNextOrderedItemsAsync(HttpClient client, int nuggetIndex)
        {
            List<ItemData> items = new List<ItemData>();

            List<NuggetData>? orderedNuggets = await GetNuggetData(client);
            if(orderedNuggets == null) return items;

            for (; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex++)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                var item = await GetItem(client, nugget.AnkamaId);
                if (item != null) items.Add(item);

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

            List<NuggetData>? orderedNuggets = await GetNuggetData(client);
            if(orderedNuggets == null) return items;

            for (; nuggetIndex < orderedNuggets.Count && nuggetIndex >= 0; nuggetIndex--)
            {
                NuggetData nugget = orderedNuggets[nuggetIndex];

                var item = await GetItem(client, nugget.AnkamaId);
                if (item != null) items.Add(item);

                if (items.Count >= ITEMS_PER_PAGE)
                {
                    return items;
                }
            }

            return items;
        }

        public static async Task<ItemData?> GetItem(HttpClient client, long ankamaId)
        {
            var categories = new string[]
            {
                "https://api.dofusdu.de/dofus2/en/items/resources/{0}",
                "https://api.dofusdu.de/dofus2/en/items/consumables/{0}",
                "https://api.dofusdu.de/dofus2/en/items/cosmetics/{0}",
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

                return itemData;
            }

            return null;
        }

        private static readonly SemaphoreSlim _GetNuggetValueSemaphore = new SemaphoreSlim(1, 1);

        public static async Task<float> GetNuggetValue(HttpClient client, long ankamaId, bool ignoreGetCache = false)
        {
            float nuggetValue = 0f;
            try
            {
                await _GetNuggetValueSemaphore.WaitAsync();
                nuggetValue = await InternalGetNuggetValue(client, ankamaId, ignoreGetCache);
            }
            catch (Exception e)
            {
                // ignored
            }
            finally
            {
                _GetNuggetValueSemaphore.Release();
            }
            return nuggetValue;
        }

        private static async Task<float> InternalGetNuggetValue(HttpClient client, long ankamaId, bool ignoreGetCache)
        {
            float nuggetValue = 0;
            ItemData? itemData = null;
            var cachePath = $"CachedNuggets/{ankamaId}";

            if (!ignoreGetCache)
            {
                if (File.Exists(cachePath))
                {
                    var txt = await File.ReadAllTextAsync(cachePath);
                    return float.Parse(txt);
                }
            }

            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null)
            {
                Console.WriteLine($"nugget.json doesn't exist");
                return 0;
            }

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
                itemData = ItemData.FromJson(data);

                //If the item doesnt exist.. something is up
                if (itemData == null)
                {
                    continue;
                }

                var nugget = nuggetData.FirstOrDefault(s => s.AnkamaId == itemData.AnkamaId);

                if (itemData.Recipe == null || nugget is { Amount: > 0 })
                {
                    nuggetValue = nugget?.Amount ?? 0;
                }
                else
                {
                    foreach (Recipe? recipeItem in itemData.Recipe)
                    {
                        float nuggetBaseValue = await InternalGetNuggetValue(client, recipeItem.ItemAnkamaId, ignoreGetCache);
                        var recipeItemData = await GetItem(client, recipeItem.ItemAnkamaId);
                        if (recipeItemData?.Recipe == null || recipeItemData.Recipe.Length == 0)
                        {
                            nuggetValue += (nuggetBaseValue * 1.0f) * recipeItem.Quantity;
                        }
                        else
                        {
                            //TODO - Could be a crafted sub-recipe bonus here...
                            nuggetValue += (nuggetBaseValue * 1.0f) * recipeItem.Quantity;
                        }
                    }
                }
            }

            if (!Directory.Exists("CachedNuggets"))
            {
                Directory.CreateDirectory("CachedNuggets");
            }

            await File.WriteAllTextAsync(cachePath, nuggetValue.ToString(CultureInfo.InvariantCulture));

            return nuggetValue;
        }
    }
}
