using Baguettefy.Data.DofusDb.NPC;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using System.Web;

namespace Baguettefy.Commands
{
    public class RequestNPCTranslateCommand : InteractionModuleBase<InteractionContext>
    {
        static readonly HttpClient _Client = new HttpClient();

        public enum ELanguage
        {
            French,
            English
        }

        [SlashCommand("translate_npc", "Search the name of an NPC and return the info", runMode: RunMode.Async)]
        public async Task GetItemName(string name)
        {
            await DeferAsync(true);

            try
            {
                var enCodEdNammE = HttpUtility.UrlEncode(name);

                var search_url = $"https://api.dofusdb.fr/npcs?name.fr={enCodEdNammE}";

                NPCData? search_item = null;
                HttpResponseMessage response = await _Client.GetAsync(search_url);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    search_item = JsonConvert.DeserializeObject<NpcDetails>(data)?.Data?.FirstOrDefault();
                }

                if (search_item == null)
                {
                    search_url = $"https://api.dofusdb.fr/npcs?name.en={enCodEdNammE}";

                    HttpResponseMessage alt_response = await _Client.GetAsync(search_url);
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await alt_response.Content.ReadAsStringAsync();
                        search_item = JsonConvert.DeserializeObject<NpcDetails>(data)?.Data?.FirstOrDefault();
                    }
                }

                if (search_item == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                    });
                    return;
                }

                var english_name = search_item.Name.En;
                var french_name = search_item.Name.Fr;

                var embedBuilder = new EmbedBuilder()
                {
                    Title = $"Search: '{name}' found..",
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