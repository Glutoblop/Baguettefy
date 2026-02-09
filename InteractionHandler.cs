using Baguettefy.Core.Interfaces;
using Baguettefy.Data.Nuggets;
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
                else if (arg.Data.CustomId.StartsWith("nugget|list"))
                {
                    var data = arg.Data.CustomId.Split("|");
                    bool isNext = data[^2] == "next";
                    var startingIndex = int.Parse(data[^1]);

                    if (startingIndex <= NuggetUtils.ITEMS_PER_PAGE && !isNext)
                    {
                        await arg.UpdateAsync(properties => { });
                        return;
                    }

                    await arg.DeferAsync(true);

                    List<NuggetData>? nuggetData = await NuggetUtils.GetNuggetData(_HttpClient);
                    if (nuggetData == null)
                    {
                        await arg.ModifyOriginalResponseAsync(properties =>
                        {
                            properties.Content = $"❌ Could not find anymore data.";
                        });
                        return;
                    }

                    var items = isNext
                        ? await NuggetUtils.GetNextOrderedItemsAsync(_HttpClient, startingIndex)
                        : await NuggetUtils.GetPreviousOrderedItemsAsync(_HttpClient, startingIndex - NuggetUtils.ITEMS_PER_PAGE - 1);

                    var embeds = new List<EmbedBuilder>();

                    foreach (var item in items)
                    {
                        embeds.Add(new EmbedBuilder()
                            .WithTitle(item.Name)
                            .WithThumbnailUrl(item.ImageUrls.Sd.AbsoluteUri)
                            .AddField("Nuggets", $"{await NuggetUtils.GetNuggetValue(_HttpClient, item.AnkamaId)}"));
                    }

                    var lastItemIndex = startingIndex + items.Count;
                    if (!isNext) lastItemIndex = startingIndex - items.Count;

                    var components = new ComponentBuilder
                    {
                        ActionRows = new List<ActionRowBuilder>()
                    {
                        new()
                        {
                            Components = new List<IMessageComponentBuilder>()
                            {
                                new ButtonBuilder()
                                    .WithCustomId($"nugget|list|prev|{lastItemIndex}")
                                    .WithLabel("Previous")
                                    .WithStyle(ButtonStyle.Success),

                                new ButtonBuilder()
                                    .WithCustomId($"nugget|list|next|{lastItemIndex}")
                                    .WithLabel("Next")
                                    .WithStyle(ButtonStyle.Success)
                            }
                        }
                    }
                    };

                    await arg.ModifyOriginalResponseAsync(properties =>
                    {
                        var pageNumber = lastItemIndex / NuggetUtils.ITEMS_PER_PAGE;
                        if (pageNumber == 0) pageNumber = 1;

                        properties.Content = $"Page: {pageNumber}/{nuggetData.Count / NuggetUtils.ITEMS_PER_PAGE}";
                        properties.Embeds = embeds.Select(s => s.Build()).ToArray();
                        properties.Components = components.Build();
                    });
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

                var db = _Services.GetRequiredService<IFirebaseDatabase>();
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
                else if (Regex.IsMatch(txt, @"\b!unleash\b"))
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
