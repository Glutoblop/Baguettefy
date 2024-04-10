using Baguettefy.Cache;
using Baguettefy.Core.Interfaces;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Baguettefy.Commands
{
    [RequireOwner]
    public class RefreshDatabaseCommands : InteractionModuleBase<InteractionContext>
    {
        private IServiceProvider _Services;

        public RefreshDatabaseCommands(IServiceProvider services)
        {
            _Services = services;
        }


        [SlashCommand("update", "Update database",
            runMode: RunMode.Async)]
        public async Task UpdateDatabase()
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IFirebaseDatabase>();
            await new UpdateDatabase().Update(db, true);

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Completed";
            });
        }
    }
}
