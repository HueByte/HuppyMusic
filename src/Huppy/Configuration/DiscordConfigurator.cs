using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord;
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
        private readonly AppSettings _appSettings;

        public DiscordConfigurator(IServiceProvider provider)
        {
            _serviceProvider = provider;

            _shardedClient = _serviceProvider.GetRequiredService<DiscordShardedClient>();
            _discordEvents = _serviceProvider.GetRequiredService<DiscordEvents>();
            _appSettings = _serviceProvider.GetRequiredService<AppSettings>();
        }

        public async Task ConfigureCommandsAsync() =>
            await _serviceProvider.GetRequiredService<CommandHandlerService>().InitializeAsync();

        public async Task InitializeBot()
        {
            // for debug
            _shardedClient.Log += (LogMessage) => { Console.WriteLine(LogMessage.Message); return Task.CompletedTask; };

            await _shardedClient.LoginAsync(TokenType.Bot, _appSettings.BotToken);
            await _shardedClient.StartAsync();

            // set basic activity 
            await _shardedClient.SetGameAsync("Prefix: ^", null, ActivityType.Playing);
        }

        public async Task ConfigureClientEventsAsync()
        {
            _shardedClient.ShardReady += _discordEvents.OnReadyAsync;
        }
    }
}