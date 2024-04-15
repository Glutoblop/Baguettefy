using Baguettefy.Core.Interfaces;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PlantUml.Net;

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

        //Short data for either Achievement or Quest info
        public class ShortData
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        public class Requirements
        {
            public ShortData QuestData { get; set; }
            public ShortData AchievementData { get; set; }

            public List<Requirements> Required { get; set; } = new List<Requirements>();

            public string ToMermaid()
            {
                string mermaid = "flowchart TD\r\n";
                int step = 0;

                UpdateMermaid(ref step, ref mermaid);

                return mermaid;
            }

            private void UpdateMermaid(ref int step, ref string mermaid)
            {
                var startStep = step;
                for (var index = 0; index < Required.Count; index++)
                {
                    var quest = Required[index];
                    //mermaid += $"\n    {startStep}({Data.Name}) --> {++step}({quest.Data.Name})";
                }

                foreach (Requirements quest in Required)
                {
                    quest.UpdateMermaid(ref step, ref mermaid);
                }
            }

            public string ToPlantUml()
            {
                string plant = "";
                UpdatePlant(0, ref plant);

                //This is  little hacky, but it lerps between the max lines and min lines
                //Using that T value it will proportionally scale up/down the size and dpi of the output to try and fit the image in
                //These values are all magic numbers using No More Mystery Ice Guy as the max and
                //then raising the min until it worked for the majority of small quests.
                //Tested in https://www.planttext.com/ to check settings

                int max = 6200;
                int min = 1000;
                var length = plant.Length;
                float t = MathUtils.NormaliseRange(length, min, max);

                int maxDpi = 24;
                int minDpi = 192;
                int dpi = (int)MathUtils.Lerp(minDpi, maxDpi, t);

                int maxHeight = 4320;
                int minHeight = 1080;
                int height = (int)MathUtils.Lerp(minHeight, maxHeight, t);

                int maxWidth = 7680;
                int minWidth = 1920;
                int width = (int)MathUtils.Lerp(minWidth, maxWidth, t);

                var startGraph = @"@startmindmap";
                var start =
                            $"skinparam dpi {dpi}\n" +
                            $"scale {height} height\n" +
                            $"scale {width} width\n";

                var value = $"{startGraph}\n{start}{plant}\n@endmindmap";


                return value;
            }

            private static void PutAsteriks(ref string value, int count)
            {
                for (int i = 0; i <= count; i++)
                {
                    value += "*";
                }
            }


            private void UpdatePlant(int step, ref string plant)
            {
                PutAsteriks(ref plant, step);
                plant += $" {(QuestData == null ? $"<:1f451:> {AchievementData.Name}" : $"<:1f4d6:> {QuestData.Name}")}\n";

                foreach (var req in Required)
                {
                    PutAsteriks(ref plant, step + 1);
                    plant +=
                        $" {(req.QuestData == null ? $"<:1f451:> {req.AchievementData.Name}" : $"<:1f4d6:> {req.QuestData.Name}")}\n";

                    foreach (Requirements childReq in req.Required)
                    {
                        childReq.UpdatePlant(step + 2, ref plant);
                    }
                }
            }

        }

        [SlashCommand("prerequisites", "Search an English/French quest name for its prerequisites.", runMode: RunMode.Async)]
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

            AchievementData? foundAchievement = null;
            await db.GetAllAsync<AchievementData>($"Achievement", (path, item) =>
            {
                if (item.Name.En.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundAchievement = item;
                    return true;
                }

                if (item.Name.Fr.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    foundAchievement = item;
                    return true;
                }

                return false;
            });

            if (foundQuest == null && foundAchievement == null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"\ud83e\udd56 Non non Baguette \ud83e\udd56\n" +
                                         $"Could not find any quest/achievement containing the phrase: {name}";
                });
                return;
            }

            try
            {
                Requirements requirements = new Requirements();
                if (foundQuest != null)
                {
                    requirements.QuestData = new ShortData()
                    {
                        Id = foundQuest.Id,
                        Name = foundQuest.Name.En
                    };
                    await PopulateQuestRequirements(db, requirements);
                }
                else
                {
                    requirements.AchievementData = new ShortData()
                    {
                        Id = foundAchievement.Id,
                        Name = foundAchievement.Name.En
                    };
                    await PopuplateAchievemnetRequiremenets(db, requirements);
                }

                //var mermaid = requirements.ToMermaid();
                var plant = requirements.ToPlantUml();

                var factory = new RendererFactory();

                var renderer = factory.CreateRenderer(new PlantUmlSettings());

                var bytes = await renderer.RenderAsync(plant, OutputFormat.Png);

                using var imgStream = new System.IO.MemoryStream(bytes);
                var channel = await Context.Guild.GetChannelAsync(Context.Interaction.ChannelId.Value);
                if (channel is ITextChannel c)
                {
                    IUserMessage? msg = null;
                    string imageName = "";
                    if (foundQuest != null)
                    {
                        imageName = foundQuest.Name.En;
                        msg = await c.SendMessageAsync($"# Quest chain found for: {foundQuest.Name.En}");
                    }
                    else
                    {
                        imageName = foundAchievement.Name.En;
                        msg = await c.SendMessageAsync($"# Achievement chain found for: {foundAchievement.Name.En}");
                    }

                    await msg.ModifyAsync(properties =>
                    {
                        properties.Attachments =
                            new[]
                            {
                                new FileAttachment(imgStream, $"{imageName}.png")
                            };
                    });
                }

                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"\ud83e\udd56 Oui Oui Baguette \ud83e\udd56\n";
                });
            }
            catch (Exception e)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"\ud83e\udd56 Non non Baguette \ud83e\udd56\n" +
                                         $"Something went wrong :(";
                });
            }
        }

        private async Task PopulateQuestRequirements(IFirebaseDatabase db, Requirements requirements)
        {
            //Qf=1942&Qf=1945&Qf=1946
            //PL>179&PO=19414

            //Qf = Quest Finished
            //PL = Level Required
            //PO = Possess ItemData

            var quest = await db.GetAsync<QuestData>($"Quest/{requirements.QuestData.Id}");

            var split = quest.StartCriterion.Split("&".ToCharArray());
            foreach (var item in split)
            {
                if (!item.StartsWith("Qf")) continue;
                var qf = "Qf=";
                var questFinished = item.Remove(0, qf.Length);
                var reqQuest = await db.GetAsync<QuestData>($"Quest/{questFinished}");
                if (reqQuest == null) continue;
                requirements.Required.Add(new Requirements()
                {
                    QuestData = new ShortData() { Id = reqQuest.Id, Name = reqQuest.Name.En }
                });
            }

            foreach (var questRequired in requirements.Required)
            {
                await PopulateQuestRequirements(db, questRequired);
            }

        }

        private async Task PopuplateAchievemnetRequiremenets(IFirebaseDatabase db, Requirements requirements)
        {
            var achievement = await db.GetAsync<AchievementData>($"Achievement/{requirements.AchievementData.Id}");

            foreach (var achObj in achievement?.Objectives ?? new List<AchievementObjective>())
            {
                var criterion = achObj.Criterion.Replace("(", "").Replace(")", "");
                var split = criterion.Split("&".ToCharArray());
                foreach (var item in split)
                {
                    if (item.StartsWith("OA"))
                    {
                        var qf = "OA=";
                        var achId = item.Remove(0, qf.Length);
                        var reqAch = await db.GetAsync<AchievementData>($"Achievement/{achId}");
                        if (reqAch == null) continue;
                        var achRequirements = new Requirements()
                        {
                            AchievementData = new ShortData() { Id = long.Parse(achId), Name = reqAch.Name.En }
                        };
                        await PopuplateAchievemnetRequiremenets(db, achRequirements);
                        requirements.Required.Add(achRequirements);
                    }

                    if (item.StartsWith("Qf"))
                    {
                        var qf = "Qf=";
                        var questFinished = item.Remove(0, qf.Length);
                        var reqQuest = await db.GetAsync<QuestData>($"Quest/{questFinished}");
                        if (reqQuest == null) continue;
                        var questRequirements = new Requirements()
                        {
                            QuestData = new ShortData() { Id = reqQuest.Id, Name = reqQuest.Name.En }
                        };
                        await PopulateQuestRequirements(db, questRequirements);
                        requirements.Required.Add(questRequirements);
                    }
                }
            }
        }

    }
}
