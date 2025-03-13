using Baguettefy.Cache;
using Baguettefy.Core.Interfaces;
using Baguettefy.Core.Logging;
using Baguettefy.Data;
using Baguettefy.Data.DofusDb.Achievements;
using Baguettefy.Data.DofusDb.Dungeons;
using Baguettefy.Data.DofusDb.Quests;
using Baguettefy.Data.Nuggets;
using Baguettefy.Data.Quests;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DofusDailyMonster.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RunMode = Discord.Interactions.RunMode;

namespace Baguettefy
{
    public class Program
    {
        private static bool clientReady = false;

        public static Task Main() => new Program().MainAsync();

        public async Task MainAsync()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc };

            //await GenerateNuggetZip.Generate();

            await NuggetUtils.Init();

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("config.json")
                    .Build();

                using IHost host = Host.CreateDefaultBuilder()
                    .ConfigureServices((_, services) => services
                        .AddSingleton(config)
                        .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
                        {
                            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                            AlwaysDownloadUsers = true
                        }))
                        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(),
                            new InteractionServiceConfig() { DefaultRunMode = RunMode.Async }))
                        .AddSingleton<InteractionHandler>()
                        .AddSingleton(x => new CommandService(new CommandServiceConfig()
                        {
                            DefaultRunMode = Discord.Commands.RunMode.Async
                        }))
                        .AddSingleton<ILogger>(s => new ConsoleLogger(ELogType.VeryVerbose))
                        .AddSingleton<IFirebaseDatabase>(s => new CachedFirebaseDatabase())
                        .AddSingleton<IDatabase>(s => new CachedDatabase())
                    ).Build();

                await RunAsync(host);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Big Crash: {e}");
                throw;
            }
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider services = serviceScope.ServiceProvider;

            var config = services.GetRequiredService<IConfigurationRoot>();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var sCommands = services.GetRequiredService<InteractionService>();
            await services.GetRequiredService<InteractionHandler>().InitialiseAsync();

            bool forceUpdate = true;
#if DEBUG
            forceUpdate = false;
#endif

            var firebase = services.GetRequiredService<IFirebaseDatabase>();
            firebase.CachedCollections = new Dictionary<string, Dictionary<string, object>>()
            {
                {"Completed", new(){{"Type", typeof(CacheComplete)}, { "ForceUpdate", forceUpdate}}},

                {"QuestCategories", new() { { "Type", typeof(AllQuestCategories) }, {"ForceUpdate", forceUpdate}}},
                {"Quest", new() { { "Type", typeof(QuestData) }, { "ForceUpdate", forceUpdate } }},

                {"AchievementCategories", new() { { "Type", typeof(AllAchievementCategories) }, { "ForceUpdate", forceUpdate } }},
                {"Achievement", new() { { "Type", typeof(AchievementData) }, { "ForceUpdate", forceUpdate } }},

                {"Dungeon", new() { { "Type", typeof(DungeonData) }, { "ForceUpdate", forceUpdate } }},
            };
            var databaseUrl = config["firebaseDatabaseUrl"];
            var serviceAccount = config["firebaseServiceAccount"];
            await firebase.Init(databaseUrl, serviceAccount, "CachedDatabase");

            var db = services.GetRequiredService<IDatabase>();
            await db.Init("LocalCache");

#if DEBUG
            //await UpdateDatabase.Update(db);
#endif

            client.Log += async (LogMessage msg) => { Console.WriteLine($"[{DateTime.Now:t}] Log: {msg}"); };
            sCommands.Log += async (LogMessage msg) => { Console.WriteLine($"[{DateTime.Now:t}] Interaction: {msg}"); };

            client.Ready += async () =>
            {
                Console.WriteLine($"Bot is ready.");
                if (clientReady) return;

                foreach (var guild in client.Guilds)
                {
                    await sCommands.RegisterCommandsToGuildAsync(guild.Id);
                    if (clientReady) continue;
                }

                clientReady = true;
            };

            client.Connected += () =>
            {
                return Task.CompletedTask;
            };

            client.Disconnected += exception =>
            {
                return Task.CompletedTask;
            };

            client.GuildAvailable += async guild =>
            {
                Console.WriteLine($"Guild available");
                if (!clientReady) return;
                await sCommands.RegisterCommandsToGuildAsync(guild.Id);
            };

            await client.LoginAsync(TokenType.Bot, config["discordtoken"]);

            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
