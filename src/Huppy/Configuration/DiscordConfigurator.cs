using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Huppy.EventHandlers;
using Huppy.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Huppy.Configuration
{
    public class DiscordConfigurator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordShardedClient _shardedClient;
        private readonly DiscordEvents _discordEvents;

        public DiscordConfigurator(IServiceProvider provider)
        {
            _serviceProvider = provider;

            _shardedClient = _serviceProvider.GetRequiredService<DiscordShardedClient>();
            _discordEvents = _serviceProvider.GetRequiredService<DiscordEvents>();
        }

        public async Task ConfigureCommandsAsync() =>
            await _serviceProvider.GetRequiredService<CommandHandlerService>().InitializeAsync();

        public async Task InitializeBot()
        {
            // for debug
            _shardedClient.Log += (LogMessage) => { Console.WriteLine(LogMessage.Message); return Task.CompletedTask; };

            await _shardedClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken", EnvironmentVariableTarget.User));
            await _shardedClient.StartAsync();
        }

        public async Task ConfigureClientEventsAsync()
        {
            _shardedClient.ShardReady += _discordEvents.OnReadyAsync;
        }
    }
}