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

            var db = _Services.GetRequiredService<IFirebaseDatabase>();

            try
            {
                DungeonData? foundDungeon = null;
                await db.GetAllAsync<DungeonData>($"Dungeon", (path, item) =>
                {
                    if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        foundDungeon = item;
                        return true;
                    }

                    if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        foundDungeon = item;
                        return true;
                    }

                    return false;
                });

                if (foundDungeon == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"Could not find any dungeon containing the phrase: {name}";
                    });
                    return;
                }

                var embedBuilder = new EmbedBuilder()
                {
                    Title = $"Quest Found",
                    ThumbnailUrl = "https://api.dofusdu.de/dofus3/v1/img/item/84719-128.png"
                };

                embedBuilder.WithFields(new[]
                {
                    new EmbedFieldBuilder()
                        .WithName($"English Name")
                        .WithValue($"{foundDungeon.Name.En}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"French Name")
                        .WithValue($"{foundDungeon.Name.Fr}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"DofusDB Quest Link")
                        .WithValue($"https://dofusdb.fr/en/database/dungeon/{foundDungeon.Id}")
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
