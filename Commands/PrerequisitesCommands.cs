using Baguettefy.Core.Interfaces;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.DofusDb.Requirements;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PlantUml.Net;

namespace Baguettefy.Commands
{
    public class PrerequisitesCommands : InteractionModuleBase<InteractionContext>
    {
        private IServiceProvider _Services;

        public PrerequisitesCommands(IServiceProvider services)
        {
            _Services = services;
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
                    requirements.QuestData = new RequirementInfo()
                    {
                        Id = foundQuest.Id,
                        Name = foundQuest.Name.En
                    };
                    await PopulateQuestRequirements(db, requirements);
                }
                else
                {
                    requirements.AchievementData = new RequirementInfo()
                    {
                        Id = foundAchievement.Id,
                        Name = foundAchievement.Name.En
                    };
                    await PopuplateAchievemnetRequiremenets(db, requirements);
                }

                // ----- 


                // ----- CREATE PLANT UML DIAGRAM

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
                    QuestData = new RequirementInfo() { Id = reqQuest.Id, Name = reqQuest.Name.En }
                });
            }

            foreach (var questRequired in requirements.Required)
            {
                await PopulateQuestRequirements(db, questRequired);
            }

        }

        private async Task PopuplateAchievemnetRequiremenets(IFirebaseDatabase db, Requirements requirements)
        {
            //Qf=1942&Qf=1945&Qf=1946
            //PL>179&PO=19414

            //Qf = Quest Finished
            //PL = Level Required
            //PO = Possess ItemData

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
                            AchievementData = new RequirementInfo() { Id = long.Parse(achId), Name = reqAch.Name.En }
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
                            QuestData = new RequirementInfo() { Id = reqQuest.Id, Name = reqQuest.Name.En }
                        };
                        await PopulateQuestRequirements(db, questRequirements);
                        requirements.Required.Add(questRequirements);
                    }
                }
            }
        }
    }
}
