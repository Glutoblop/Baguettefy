using Baguettefy.Core.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Baguettefy
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _Services;

        private ILogger _Logger;

        static readonly HttpClient _HttpClient = new HttpClient();

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _Services = services;

            _Logger = services.GetRequiredService<ILogger>();
        }

        public async Task InitialiseAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _Services);
            _client.InteractionCreated += HandleInteraction;
            _client.ButtonExecuted += HandleButtonPressed;
            _client.ModalSubmitted += HandleModalSubmitted;

            _client.MessageReceived += HandleMessageReceived;
            _client.MessageUpdated += HandleMessageUpdated;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            _Logger?.Log($"[HandleInteraction]", ELogType.Log);
            var dialogueContext = new InteractionContext(_client, arg);
            await _commands.ExecuteCommandAsync(dialogueContext, _Services);
        }

        private async Task HandleButtonPressed(SocketMessageComponent arg)
        {
            _Logger?.Log($"[HandleButtonPressed]", ELogType.Log);
            try
            {
                if (arg.Data.CustomId.StartsWith("Translate"))
                {
                    var subType = arg.Data.CustomId.Split("-")[1];
                    switch (subType)
                    {
                        case "Quest":
                        case "Achievement":
                        case "Item":
                        case "Dungeon":
                            {
                                var modalBuilder = new ModalBuilder()
                                .WithTitle($"Enter in either French or English")
                                .WithCustomId($"Translate-{subType}")
                                .AddTextInput($"What {subType} do you want translated?", $"Translate-{subType}-Input");

                                await arg.RespondWithModalAsync(modalBuilder.Build());

                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                //ignored
            }
        }

        private async Task HandleModalSubmitted(SocketModal modal)
        {
            _Logger?.Log($"[HandleModalSubmitted]", ELogType.Log);

            if (modal.Data?.CustomId?.StartsWith("Translate-") ?? false)
            {
                await modal.DeferAsync(true);
                var msg = await modal.FollowupAsync($"Thinking.. 💭", ephemeral: true);

                var db = _Services.GetRequiredService<IDatabase>();
                EmbedBuilder embedBuilder = null;
                var value = modal.Data.Components.First().Value;

                var subType = modal.Data.CustomId.Split("-")[1];
                switch (subType)
                {
                    case "Quest":
                        {
                            embedBuilder = await FindTranslationData.FindQuest(db, value);
                            break;
                        }
                    case "Achievement":
                        {
                            embedBuilder = await FindTranslationData.FindAchievement(db, value);
                            break;
                        }
                    case "Item":
                        {
                            embedBuilder = await FindTranslationData.FindItem(value);
                            break;
                        }
                    case "Dungeon":
                        {
                            embedBuilder = await FindTranslationData.FindDungeon(db, value);
                            break;
                        }
                }

                if (embedBuilder != null)
                {
                    await msg.ModifyAsync(p =>
                    {
                        p.Content = $"\U0001f956 Oui Oui Baguette \U0001f956";
                        p.Embed = embedBuilder.Build();
                    });
                }
                else
                {
                    await msg.ModifyAsync(p =>
                    {
                        p.Content = $"\U0001f956 Non non Baguette \U0001f956\nSomething went wrong :(";
                    });
                }
            }
        }

        private async Task HandleMessageReceived(SocketMessage message)
        {
            try
            {
                var txt = message.Content.Trim().ToLowerInvariant();

                if (Regex.IsMatch(txt, @"\bshadow\b"))
                {
                    await message.AddReactionAsync(new Emoji("🥳"));
                }
                if (txt.Contains("unleash"))
                {
                    await message.AddReactionAsync(new Emoji("🐺"));
                }
            }
            catch (Exception e)
            {

            }
        }

        private async Task HandleMessageUpdated(Cacheable<IMessage, ulong> cacheable, SocketMessage message, ISocketMessageChannel channel)
        {
            await HandleMessageReceived(message);
        }

    }
}
