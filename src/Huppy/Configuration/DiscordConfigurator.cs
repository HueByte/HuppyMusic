using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Huppy.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Huppy.Configuration
{
    public class DiscordConfigurator
    {
        private readonly IServiceProvider _serviceProvider;
        public DiscordConfigurator(IServiceProvider provider)
        {
            _serviceProvider = provider;
        }

        public async Task ConfigureCommandsAsync() =>
            await _serviceProvider.GetRequiredService<CommandHandlerService>().InitializeAsync();

        public async Task InitializeBot()
        {
            var shardedClient = _serviceProvider.GetRequiredService<DiscordShardedClient>();

            // for debug
            shardedClient.Log += (LogMessage) => { Console.WriteLine(LogMessage.Message); return Task.CompletedTask; };

            await shardedClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken", EnvironmentVariableTarget.User));
            await shardedClient.StartAsync();
        }
    }
}