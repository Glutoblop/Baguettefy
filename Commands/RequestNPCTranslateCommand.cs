using Discord;
using Discord.Interactions;

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

            EmbedBuilder embedBuilder = await FindTranslationData.GetItemName(name);

            if (embedBuilder != null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $" \ud83e\udd56 Oui Oui Baguette \ud83e\udd56";
                    properties.Embed = embedBuilder.Build();
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $" \ud83e\udd56 Non non Baguette \ud83e\udd56\nSomething went wrong :(";
                });
            }
        }
    }
}