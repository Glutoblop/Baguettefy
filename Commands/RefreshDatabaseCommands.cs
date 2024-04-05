using Discord.Interactions;

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

        }
    }
}
