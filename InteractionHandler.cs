using System.Reflection;
using Baguettefy.Core.Interfaces;
using Baguettefy.Data.Items;
using Baguettefy.Data.Nuggets;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Baguettefy
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

        private ILogger _Logger;

        static readonly HttpClient _HttpClient = new HttpClient();

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;

            _Logger = services.GetRequiredService<ILogger>();
        }

        public async Task InitialiseAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.InteractionCreated += HandleInteraction;
            _client.ButtonExecuted += HandleButtonPressed;
        }

        private async Task HandleButtonPressed(SocketMessageComponent arg)
        {
            _Logger?.Log($"[HandleButtonPressed]", ELogType.Log);
            try
            {
                if (!arg.Data.CustomId.StartsWith("nugget|list")) return;
                var data = arg.Data.CustomId.Split("|");
                bool isNext = data[^2] == "next";
                var startingIndex = int.Parse(data[^1]);

                if (startingIndex <= NuggetData.ITEMS_PER_PAGE && !isNext)
                {
                    await arg.UpdateAsync(properties => { });
                    return;
                }

                await arg.DeferAsync(true);

                List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
                if (nuggetData == null)
                {
                    await arg.ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"❌ Could not find anymore data.";
                    });
                    return;
                }

                var items = isNext
                    ? await NuggetData.GetNextOrderedItemsAsync(_HttpClient, startingIndex)
                    : await NuggetData.GetPreviousOrderedItemsAsync(_HttpClient, startingIndex-NuggetData.ITEMS_PER_PAGE-1);

                var embeds = new List<EmbedBuilder>();

                foreach (var item in items)
                {
                    var nugget = nuggetData.FirstOrDefault(s => s.AnkamaId == item.AnkamaId);
                    if(nugget == null) continue;

                    embeds.Add(new EmbedBuilder()
                        .WithTitle(item.Name)
                        .WithThumbnailUrl(item.ImageUrls.Sd.AbsoluteUri)
                        .AddField("Nuggets", $"{nugget.Ratio}"));
                }

                var lastItemIndex = startingIndex + items.Count;
                if (!isNext) lastItemIndex = startingIndex - items.Count;

                var components = new ComponentBuilder
                {
                    ActionRows = new List<ActionRowBuilder>()
                    {
                        new()
                        {
                            Components = new List<IMessageComponent>()
                            {
                                new ButtonBuilder()
                                    .WithCustomId($"nugget|list|prev|{lastItemIndex}")
                                    .WithLabel("Previous")
                                    .WithStyle(ButtonStyle.Success).Build(),

                                new ButtonBuilder()
                                    .WithCustomId($"nugget|list|next|{lastItemIndex}")
                                    .WithLabel("Next")
                                    .WithStyle(ButtonStyle.Success).Build()
                            }
                        }
                    }
                };

                await arg.ModifyOriginalResponseAsync(properties =>
                {
                    var pageNumber = lastItemIndex/NuggetData.ITEMS_PER_PAGE;
                    if (pageNumber == 0) pageNumber = 1;

                    properties.Content = $"Page: {pageNumber}/{nuggetData.Count/NuggetData.ITEMS_PER_PAGE}";
                    properties.Embeds = embeds.Select(s => s.Build()).ToArray();
                    properties.Components = components.Build();
                });

            }
            catch (Exception ex)
            {
                //ignored
            }
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            _Logger?.Log($"[HandleInteraction]", ELogType.Log);
            var dialogueContext = new InteractionContext(_client, arg);
            await _commands.ExecuteCommandAsync(dialogueContext, _services);
        }
    }
}
