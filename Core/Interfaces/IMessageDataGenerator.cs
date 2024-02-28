using Discord;

namespace Baguettefy.Core.Interfaces
{
    public class ModMessageData
    {
        public string Content;
        public EmbedBuilder Embed;
        public ComponentBuilder Buttons;
    }

    public interface IMessageDataGenerator
    {
        Task<ModMessageData> CreateMessageData(IDiscordClient client, IUser user, IServiceProvider services, bool compact = false);
    }
}
