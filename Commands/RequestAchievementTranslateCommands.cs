using Baguettefy.Core.Interfaces;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Baguettefy.Commands
{
    public class RequestAchievementTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        private IServiceProvider _Services;

        public RequestAchievementTranslateCommands(IServiceProvider services)
        {
            _Services = services;
        }

        [SlashCommand("translate_achieve", "Search an Achievement either English or French.", runMode: RunMode.Async)]
        public async Task TranslateAchievement(string name)
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IFirebaseDatabase>();

            try
            {
                var embedBuilder = await FindTranslationData.FindAchievement(db, name);

                if (embedBuilder == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"Could not find any Achievement containing the phrase: {name}";
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
