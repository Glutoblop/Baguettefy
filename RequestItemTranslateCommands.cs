using Discord.Interactions;
using Baguettefy.Data;
using Newtonsoft.Json;

namespace Baguettefy
{
    public class RequestItemTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        static HttpClient client = new HttpClient();

        [SlashCommand("baugettefy","Get the French name for a given English item's name.",runMode: RunMode.Async)]
        public async Task Baugettefy(string name)
        {
            await DeferAsync(true);

            var english_url = $"https://api.dofusdu.de/dofus2/en/items/search?query={name}&limit=1";

            Item? item = null;
            HttpResponseMessage response = await client.GetAsync(english_url);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                item = JsonConvert.DeserializeObject<Item[]>(data)?.FirstOrDefault();
            }
            
            if (item == null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Sorry there was a problem, try again.";
                });
                return;
            }

            var itemType = item.ItemSubtype switch
            {
                "consumables" => "items/consumables",
                "cosmetics" => "items/cosmetics",
                "resources" => "items/resources",
                "equipment" => "items/equipment",
                "quest_items" => "items/quest_items",
                "mounts" => "mounts",
                "sets" => "sets",
                _ => ""
            };

            var french_url = $"https://api.dofusdu.de/dofus2/fr/{itemType}/{item.AnkamaId}";
            
            Item? french_item = null;
            HttpResponseMessage french_response = await client.GetAsync(french_url);
            if (response.IsSuccessStatusCode)
            {
                string data = await french_response.Content.ReadAsStringAsync();
                french_item = JsonConvert.DeserializeObject<Item>(data);
            }
            
            if (french_item == null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Sorry there was a problem, try again.";
                });
                return;
            }

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $" \ud83e\udd56 Oui Oui Baguette \ud83e\udd56: {french_item.Name}";
            });

        }
    }
}
