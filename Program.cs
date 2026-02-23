using Baguettefy.Cache;
using Baguettefy.Core.Interfaces;
using Baguettefy.Core.Logging;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
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

        public static Task Main(string[] args) => new Program().MainAsync(args);

        static bool HasFlag(string[] args, string flag)
        {
            return args.Any(a =>
                string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
        }

        public async Task MainAsync(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc };

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
                            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
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
                        .AddSingleton<IDatabase>(s => new CachedDatabase())
                    ).Build();

                await RunAsync(args, host);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Big Crash: {e}");
                throw;
            }
        }

        public async Task RunAsync(string[] args, IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider services = serviceScope.ServiceProvider;

            var config = services.GetRequiredService<IConfigurationRoot>();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var sCommands = services.GetRequiredService<InteractionService>();
            await services.GetRequiredService<InteractionHandler>().InitialiseAsync();

            var db = services.GetRequiredService<IDatabase>();
            await db.Init("LocalCache");

            bool force = HasFlag(args, "-forceupdate");
            await UpdateDatabase.Update(db, force); 

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
