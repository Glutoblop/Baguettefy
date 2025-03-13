using Discord;
using Discord.Interactions;

namespace Baguettefy.Commands
{
    public class RequestItemTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        [SlashCommand("translate_item", "Search the name of an item either French/English, return info.", runMode: RunMode.Async)]
        public async Task GetItemName(string name)
        {
            await DeferAsync(true);

            try
            {
                EmbedBuilder embedBuilder = await FindTranslationData.FindItem(name);
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
