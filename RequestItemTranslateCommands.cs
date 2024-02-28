using Discord.Interactions;
using Baguettefy.Data;
using Discord;
using Newtonsoft.Json;

namespace Baguettefy
{
    public class RequestItemTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        static HttpClient client = new HttpClient();

        [SlashCommand("baguettefy", "Get the French name for a given English item's name.", runMode: RunMode.Async)]
        public async Task GetEnglishName(string name)
        {
            await DeferAsync(true);
            try
            {
                var french_search_url = $"https://api.dofusdu.de/dofus2/fr/items/search?query={name}&limit=1";

                Item? search_item = null;
                HttpResponseMessage response = await client.GetAsync(french_search_url);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    search_item = JsonConvert.DeserializeObject<Item[]>(data)?.FirstOrDefault();
                }

                if (search_item == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                    });
                    return;
                }

                var itemType = search_item.ItemSubtype switch
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

                var english_url = $"https://api.dofusdu.de/dofus2/en/{itemType}/{search_item.AnkamaId}";

                Item? english_item = null;
                HttpResponseMessage english_response = await client.GetAsync(english_url);
                if (response.IsSuccessStatusCode)
                {
                    string data = await english_response.Content.ReadAsStringAsync();
                    english_item = JsonConvert.DeserializeObject<Item>(data);
                }

                if (english_item == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                    });
                    return;
                }


                var french_url = $"https://api.dofusdu.de/dofus2/fr/{itemType}/{search_item.AnkamaId}";

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
                        properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                    });
                    return;
                }

                var embedBuilder = new EmbedBuilder()
                {
                    Title = $"{english_item.Name}",
                    ThumbnailUrl = english_item.ImageUrls.Icon?.AbsoluteUri,
                };

                embedBuilder.WithFields(new[]
                {
                new EmbedFieldBuilder()
                    .WithName($"Name")
                    .WithValue($"{french_item.Name}")
                    .WithIsInline(false),

                new EmbedFieldBuilder()
                    .WithName($"Category")
                    .WithValue($"{search_item.ItemSubtype}")
                    .WithIsInline(false),

                new EmbedFieldBuilder()
                    .WithName($"Description")
                    .WithValue($"{english_item.Description}")
                    .WithIsInline(false)

            });

                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $" \ud83e\udd56 Oui Oui Baguette \ud83e\udd56";
                    properties.Embed = embedBuilder.Build();
                });
            }
            catch (Exception e)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                });
            }

        }
    }
}
