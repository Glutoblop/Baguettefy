using Discord;
using Discord.WebSocket;

namespace Baguettefy.Core.Interfaces
{
    public interface IRoleAssigner
    {
        Task Init(IDiscordClient client);

        public Task<bool> HandleReactionAdded(Cacheable<IUserMessage, ulong> msgCache,
            Cacheable<IMessageChannel, ulong> channelCache, SocketReaction reaction);

        Task<bool> HandleReactionRemoved(Cacheable<IUserMessage, ulong> msgCache, Cacheable<IMessageChannel, ulong> channelCache, SocketReaction reaction);
    }
}
