using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Huppy.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Huppy
{
    class Program
    {
        private readonly IServiceProvider _serviceProvider;
        public Program()
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Verbose,
                    CaseSensitiveCommands = false,
                    ThrowOnError = false
                }))
                .AddSingleton<CommandHandlerService>()
                .AddSingleton(new HttpClient())
                .BuildServiceProvider();
            // .AddLavaNode();
        }

        private static async Task Main()
        {
            try
            {
                await new Program().StartAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private async Task StartAsync()
        {
            //load command service
            var commandService = _serviceProvider.GetRequiredService<CommandService>();
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            var test = _serviceProvider.GetRequiredService<CommandHandlerService>();

            //connect to API
            var shardedClient = _serviceProvider.GetRequiredService<DiscordShardedClient>();

            shardedClient.Log += (LogMessage) => { Console.WriteLine(LogMessage.Message); return Task.CompletedTask; };

            await shardedClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken", EnvironmentVariableTarget.User));
            await shardedClient.StartAsync();

            await Task.Delay(-1);
        }
    }
}
