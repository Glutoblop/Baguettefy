using Baguettefy.Core.Interfaces;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Baguettefy.Commands
{
    public class RequestQuestTranslateCommands : InteractionModuleBase<InteractionContext>
    {
        static HttpClient client = new HttpClient();

        private IServiceProvider _Services;

        public RequestQuestTranslateCommands(IServiceProvider services)
        {
            _Services = services;
        }

        [SlashCommand("translate_quest", "Search a Quest either English or French.", runMode: RunMode.Async)]
        public async Task TranslateQuest(string name)
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IFirebaseDatabase>();

            try
            {
                QuestData? foundQuest = null;
                await db.GetAllAsync<QuestData>($"Quest", (path, item) =>
                {
                    if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        foundQuest = item;
                        return true;
                    }

                    if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        foundQuest = item;
                        return true;
                    }

                    return false;
                });

                if (foundQuest == null)
                {
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"Could not find any quest containing the phrase: {name}";
                    });
                    return;
                }

                var embedBuilder = new EmbedBuilder()
                {
                    Title = $"Quest Found",
                    ThumbnailUrl = "https://api.dofusdu.de/dofus2/img/item/25130-800.png"
                };

                embedBuilder.WithFields(new[]
                {
                    new EmbedFieldBuilder()
                        .WithName($"English Name")
                        .WithValue($"{foundQuest.Name.En}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"French Name")
                        .WithValue($"{foundQuest.Name.Fr}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"DofusDB Quest Link")
                        .WithValue($"https://dofusdb.fr/en/database/quest/{foundQuest.Id}")
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

        private QuestData ContainsName(AllQuestsData allQuests, string name)
        {
            if (allQuests?.Quests == null) return null;

            foreach (var quest in allQuests.Quests)
            {
                if (!ContainsName(quest, name)) continue;
                return quest;
            }

            return null;
        }

        private bool ContainsName(QuestData questData, string name)
        {
            if (questData == null) return false;
            var questName = questData.Name.En.ToLowerInvariant();
            if (questName.Contains(name)) return false;
            if (questData.Steps == null) return false;

            foreach (Step? questStep in questData.Steps)
            {
                var stepName = questStep.Name.En.ToLowerInvariant();
                if (!stepName.Contains(name)) continue;
                return true;
            }

            return false;
        }

        public class ShortQuestData
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public ShortQuestData(QuestData questData)
            {
                Id = questData.Id;
                Name = questData.Name.En;
            }
        }

        public class QuestRequirements
        {
            public ShortQuestData Quest { get; set; }

            public int LevelRequired { get; set; }

            public List<String> ItemsRequired { get; set; } = new List<String>();

            public List<QuestRequirements> QuestsRequired { get; set; } = new List<QuestRequirements>();
        }

        [SlashCommand("quest_prerequisites", "Search an English quest name for its prerequisites.", runMode: RunMode.Async)]
        public async Task QuestPrerequisites(string name)
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IFirebaseDatabase>();

            QuestData? foundQuest = null;
            await db.GetAllAsync<QuestData>($"Quest", (path, item) =>
            {
                if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundQuest = item;
                    return true;
                }

                if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundQuest = item;
                    return true;
                }

                return false;
            });

            if (foundQuest == null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Could not find any quest containing the phrase: {name}";
                });
                return;
            }

            QuestRequirements requirements = new QuestRequirements()
            {
                Quest = new ShortQuestData(foundQuest)
            };
            await PopulateRequirements(db, requirements);

            Console.WriteLine($"{requirements}");
        }

        private async Task PopulateRequirements(IFirebaseDatabase db, QuestRequirements requirements)
        {
            //Qf=1942&Qf=1945&Qf=1946
            //PL>179&PO=19414

            //Qf = Quest Finished
            //PL = Level Required
            //PO = Possess ItemData

            var quest = await db.GetAsync<QuestData>($"Quest/{requirements.Quest.Id}");

            var split = quest.StartCriterion.Split("&".ToCharArray());
            foreach (var item in split)
            {
                if (item.StartsWith("PL"))
                {
                    var pl = "PL>";
                    var levelReq = item.Remove(0, pl.Length);
                    if (int.TryParse(levelReq, out int level))
                    {
                        requirements.LevelRequired = level;
                    }
                }

                if (item.StartsWith("PO"))
                {
                    var pl = "PO=";
                    var itemIdReq = item.Remove(0, pl.Length);

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync($"https://api.dofusdb.fr/items/{itemIdReq}?lang=en");
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();
                            QuestItemData? itemData = JsonConvert.DeserializeObject<QuestItemData>(json);
                            if (itemData != null)
                            {
                                requirements.ItemsRequired.Add(itemData.Name.En);
                            }
                            else
                            {
                                requirements.ItemsRequired.Add(itemIdReq);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        requirements.ItemsRequired.Add(itemIdReq);
                    }
                }

                if (item.StartsWith("Qf"))
                {
                    var qf = "Qf=";
                    var questFinished = item.Remove(0, qf.Length);
                    var reqQuest = await db.GetAsync<QuestData>($"Quest/{questFinished}");
                    if (reqQuest != null)
                    {
                        requirements.QuestsRequired.Add(new QuestRequirements()
                        {
                            Quest = new ShortQuestData(reqQuest)
                        });
                    }
                }
            }

            foreach (var questRequired in requirements.QuestsRequired)
            {
                await PopulateRequirements(db, questRequired);
            }

        }
    }
}
