using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Huppy.Configuration;
using Huppy.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Huppy
{
    class Program
    {
        private readonly IServiceProvider _serviceProvider;
        public Program() =>
            _serviceProvider = new Configurator()
                .AddDiscord()
                .AddDiscord()
                .AddAudio()
                .AddServices()
                .BuildServiceProvider();
        private static async Task Main() => await new Program().StartAsync();

        private async Task StartAsync()
        {
            var discordConfigurator = new DiscordConfigurator(_serviceProvider);

            await discordConfigurator.ConfigureCommandsAsync();

            await discordConfigurator.InitializeBot();

            await Task.Delay(-1);
        }
    }
}
