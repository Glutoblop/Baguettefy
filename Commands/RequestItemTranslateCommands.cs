using Baguettefy.Data.Items;
using Discord.Interactions;
using Discord;
using Newtonsoft.Json;

namespace Baguettefy.Commands
{
    public class RequestItemTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        static HttpClient client = new HttpClient();

        public enum ELanguage
        {
            French,
            English
        }

        [SlashCommand("translate_item", "Search the name of an item either French/English, return info.", runMode: RunMode.Async)]
        public async Task GetItemName(string name, ELanguage inputLanguage = ELanguage.French)
        {
            await TranslateItem(name, inputLanguage == ELanguage.English);
        }


        private async Task TranslateItem(string name, bool isEnglish)
        {
            await DeferAsync(true);

            string incomingLang = isEnglish ? "en" : "fr";
            string otherLang = !isEnglish ? "en" : "fr";

            try
            {
                var initial_search_url = $"https://api.dofusdu.de/dofus2/{incomingLang}/items/search?query={name}&limit=1";

                ItemData? search_item = null;
                HttpResponseMessage response = await client.GetAsync(initial_search_url);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    search_item = JsonConvert.DeserializeObject<ItemData[]>(data)?.FirstOrDefault();
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

                var detail_url = $"https://api.dofusdu.de/dofus2/{otherLang}/{itemType}/{search_item.AnkamaId}";

                ItemData? detail_item = null;
                HttpResponseMessage detail_response = await client.GetAsync(detail_url);
                if (response.IsSuccessStatusCode)
                {
                    string data = await detail_response.Content.ReadAsStringAsync();
                    detail_item = JsonConvert.DeserializeObject<ItemData>(data);
                }

                if (detail_item == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                    });
                    return;
                }


                var alt_url = $"https://api.dofusdu.de/dofus2/{incomingLang}/{itemType}/{search_item.AnkamaId}";

                ItemData? alt_item = null;
                HttpResponseMessage alt_response = await client.GetAsync(alt_url);
                if (response.IsSuccessStatusCode)
                {
                    string data = await alt_response.Content.ReadAsStringAsync();
                    alt_item = JsonConvert.DeserializeObject<ItemData>(data);
                }

                if (alt_item == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                    });
                    return;
                }

                var english_name = isEnglish ? search_item.Name : detail_item.Name;
                var french_name = !isEnglish ? search_item.Name : detail_item.Name;

                var embedBuilder = new EmbedBuilder()
                {
                    Title = $"Search: '{name}' found..",
                    ThumbnailUrl = detail_item.ImageUrls.Icon?.AbsoluteUri,
                };

                embedBuilder.WithFields(new[]
                {
                new EmbedFieldBuilder()
                    .WithName($"French Name")
                    .WithValue($"{french_name}")
                    .WithIsInline(false),

                new EmbedFieldBuilder()
                    .WithName($"English Name")
                    .WithValue($"{english_name}")
                    .WithIsInline(false),

                new EmbedFieldBuilder()
                    .WithName($"Category")
                    .WithValue($"{search_item.ItemSubtype}")
                    .WithIsInline(false),

                new EmbedFieldBuilder()
                    .WithName($"Description")
                    .WithValue($"{detail_item.Description}")
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
