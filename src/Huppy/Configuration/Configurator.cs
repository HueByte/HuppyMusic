using System;
using System.Net.Http;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Huppy.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Huppy.Configuration
{
    public class Configurator
    {
        private readonly IServiceCollection _services;
        public Configurator(IServiceCollection services = null)
        {
            _services = services ?? new ServiceCollection();
        }

        public Configurator AddDiscord()
        {
            _services
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
                }));

            return this;
        }

        public Configurator AddAudio()
        {
            _services.AddLavaNode(lavaConfig =>
            {
                lavaConfig.SelfDeaf = false;
            });

            return this;
        }

        public Configurator AddServices()
        {
            // services with reflection
            _services.AddSingletons<IInjectableSingleton>();

            _services.AddSingleton(new HttpClient());

            return this;
        }

        public IServiceProvider BuildServiceProvider() => _services.BuildServiceProvider();
    }
}