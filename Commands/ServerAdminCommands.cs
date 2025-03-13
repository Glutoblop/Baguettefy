using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DofusDailyMonster.Core.Interfaces;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;

namespace DofusDailyMonster.Commands
{
    [Discord.Commands.RequireContext(Discord.Commands.ContextType.Guild)]
    [Discord.Commands.RequireUserPermission(ChannelPermission.ManageChannels)]
    public class ServerAdminCommands : InteractionModuleBase<InteractionContext>
    {
        private readonly IServiceProvider _Services;

        public ServerAdminCommands(IServiceProvider services)
        {
            _Services = services;
        }

        [SlashCommand("host_translations", "This channel will host the message people can use to trigger translations with buttons.")]
        public async Task HostQuizMeHere()
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IDatabase>();

            var componentBuilder = new ComponentBuilder();
            var buttons = new List<IMessageComponent>()
                    {
                        new ButtonBuilder("Quest", "Translate-Quest", ButtonStyle.Success, emote: new Emoji("📕")).Build(),
                        new ButtonBuilder("Achievement", "Translate-Achievement", ButtonStyle.Success, emote: new Emoji("🏆")).Build(),
                        new ButtonBuilder("Item", "Translate-Item", ButtonStyle.Success, emote: new Emoji("🛠️")).Build(),
                        new ButtonBuilder("Dungeon Name", "Translate-Dungeon", ButtonStyle.Success, emote: new Emoji("💀")).Build(),
                        new ButtonBuilder("Prerequisites", "Translate-Prereq", ButtonStyle.Success, emote: new Emoji("📃")).Build(),
                    };
            var actionRows = new List<ActionRowBuilder> { new ActionRowBuilder().WithComponents(buttons) };
            componentBuilder.WithRows(actionRows);

            var client = _Services.GetRequiredService<DiscordSocketClient>() as IDiscordClient;
            var guild = await client.GetGuildAsync(Context.Interaction.GuildId.Value);
            var guildChannel = await guild.GetChannelAsync(Context.Interaction.ChannelId.Value) as ITextChannel;

            if (guildChannel != null)
            {
                // Generate Monster Image Attachment

                HttpClient httpClient = new HttpClient();

                Stream fileStream = await httpClient.GetStreamAsync($"https://dofensive.com/asset/dofensive/monsters/4782");//Bwork Magus
                await Task.Delay(100);
                using MemoryStream ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                var image = ms.ToArray();

                using MagickImage magickImage = new MagickImage(image);

                QuantizeSettings settings = new QuantizeSettings();
                settings.Colors = 256;
                magickImage.Quantize(settings);

                await using Stream pngStream = new MemoryStream();
                await magickImage.WriteAsync(pngStream);

                // ---- End of Monster Image generation

                var msg = await guildChannel.SendMessageAsync($"# Baguettefy Translation Helper" +
                    $"\nBaguettefy is a Discord translation bot only available inside The Pub.\n\n" +
                    $"It provides English -> French or French -> English translations using the correct Ankama created translation.\n\n" +
                    $"Google Translate gets you most of the way, but if you need a Quest, Achievement, Resource, Item, NPC or Dungeon Name with an " +
                    $"exact translation, those translations might not match Ankama's wording perfectly\n\n" +
                    $"This is where we come in, use one of the buttons below to find the translation you need.\n" +
                    $"You can put in French or English and it will give you a private message reply giving you a breakdown of both languages it finds.\n\n\n" +
                    $"⏳May Xelor's Clock Tick For You⌛\n\n\n",
                    components: componentBuilder.Build());

                await msg.ModifyAsync(properties =>
                {
                    properties.Attachments = new Optional<IEnumerable<FileAttachment>>(new List<FileAttachment>()
                        { new(pngStream, "Monster.png") });
                });
            }

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Completed.";
            });
        }

    }
}
