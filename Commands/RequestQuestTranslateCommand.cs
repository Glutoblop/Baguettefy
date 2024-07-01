using Baguettefy.Core.Interfaces;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Baguettefy.Commands
{
    public class RequestQuestTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        private IServiceProvider _Services;

        public RequestQuestTranslateCommands(IServiceProvider services)
        {
            _Services = services;
        }

        [SlashCommand("translate_quest", "Search a Quest either English or French.", runMode: RunMode.Async)]
        public async Task TranslateQuest(string name)
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IFirebaseDatabase>();

            try
            {
                QuestData? foundQuest = null;
                await db.GetAllAsync<QuestData>($"Quest", (path, item) =>
                {
                    if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        foundQuest = item;
                        return true;
                    }

                    if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        foundQuest = item;
                        return true;
                    }

                    return false;
                });

                if (foundQuest == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"Could not find any quest containing the phrase: {name}";
                    });
                    return;
                }

                var embedBuilder = new EmbedBuilder()
                {
                    Title = $"Quest Found",
                    ThumbnailUrl = "https://api.dofusdu.de/dofus2/img/item/25130-800.png"
                };

                embedBuilder.WithFields(new[]
                {
                    new EmbedFieldBuilder()
                        .WithName($"English Name")
                        .WithValue($"{foundQuest.Name.En}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"French Name")
                        .WithValue($"{foundQuest.Name.Fr}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"DofusDB Quest Link")
                        .WithValue($"https://dofusdb.fr/en/database/quest/{foundQuest.Id}")
                        .WithIsInline(false),

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
