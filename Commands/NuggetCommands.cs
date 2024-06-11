using Baguettefy.Data.Items;
using Baguettefy.Data.Nuggets;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;

namespace Baguettefy.Commands
{
    public class NuggetCommands : InteractionModuleBase<InteractionContext>
    {
        static readonly HttpClient _Client = new HttpClient();


        [SlashCommand("item_nuggets", "Aproximately how many nuggets does this item generate?", runMode: RunMode.Async)]
        public async Task GetItemNuggets(
            [Summary(name: "itemName", description: "The name of the in English.")] string itemName)
        {
            await DeferAsync(true);

            try
            {
                List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));

                if (nuggetData != null)
                {
                    var searchUrl = $"https://api.dofusdu.de/dofus2/en/items/search?query={itemName}&limit=1";

                    HttpResponseMessage response = await _Client.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        ItemData? searchItem = JsonConvert.DeserializeObject<ItemData[]>(data)?.FirstOrDefault();
                        if (searchItem != null)
                        {
                            var nugget = nuggetData.FirstOrDefault(s => s.AnkamaId == searchItem.AnkamaId);
                            if (nugget != null)
                            {
                                await ModifyOriginalResponseAsync(properties =>
                                {
                                    properties.Content =
                                        $"[{searchItem.Name}](https://dofusdb.fr/en/database/object/{searchItem.AnkamaId}) has a Nugget ratio of {nugget.Ratio}";
                                });
                                return;
                            }
                        }

                    }
                }

            }
            catch (Exception e)
            {
                //ignored.
            }

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Could not find data on item {itemName}.";
            });
        }

        [SlashCommand("list-nuggets", "Lists all the Nugget/Item Ratios in descending order.", runMode: RunMode.Async)]
        public async Task ListNuggets()
        {
            await DeferAsync(true);

            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Internal Error, the nuggets have all ran away. ⛏";
                });
                return;
            }

            List<Tuple<ItemData, NuggetData>> items = await NuggetData.GetNextOrderedItemsAsync(_Client, nuggetData, 0);

            if (items.Count == 0)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Could not find anymore items.";
                });
                return;
            }

            var embeds = new List<EmbedBuilder>();

            foreach (var item in items)
            {
                embeds.Add(new EmbedBuilder()
                    .WithTitle(item.Item1.Name)
                    .WithThumbnailUrl(item.Item1.ImageUrls.Hd.AbsoluteUri)
                    .AddField("Nuggets", $"{item.Item2.Ratio}"));
            }

            var components = new ComponentBuilder
            {
                ActionRows = new List<ActionRowBuilder>()
                {
                    new()
                    {
                        Components = new List<IMessageComponent>()
                        {
                            new ButtonBuilder()
                                .WithCustomId($"nugget|list|prev|{NuggetData.ITEMS_PER_PAGE}")
                                .WithLabel("Prev")
                                .WithStyle(ButtonStyle.Success).Build(),

                            new ButtonBuilder()
                                .WithCustomId($"nugget|list|next|{NuggetData.ITEMS_PER_PAGE}")
                                .WithLabel("Next")
                                .WithStyle(ButtonStyle.Success).Build()
                        }
                    }
                }
            };

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Page: 1/{nuggetData.Count/NuggetData.ITEMS_PER_PAGE}";
                properties.Embeds = embeds.Select(s => s.Build()).ToArray();
                properties.Components = components.Build();
            });
        }
    }
}
