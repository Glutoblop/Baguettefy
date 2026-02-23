using Baguettefy.Core.Interfaces;
using Baguettefy.Data.DofusDb.Dungeons;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Baguettefy.Commands
{
    public class RequestDungeonTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        private IServiceProvider _Services;

        public RequestDungeonTranslateCommands(IServiceProvider services)
        {
            _Services = services;
        }

        [SlashCommand("translate_dungeon", "Search a Dungeon either English or French.", runMode: RunMode.Async)]
        public async Task TranslateDungeon(string name)
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IDatabase>();

            try
            {
                EmbedBuilder embedBuilder = await FindTranslationData.FindDungeon(db, name);

                if (embedBuilder == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"Could not find any dungeon containing the phrase: {name}";
                    });
                    return;
                }

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
