using System;
using System.Threading.Tasks;
using Huppy.Configuration;

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
                .AddServices()  // services that inherit from IInjectableSingleton get injected via reflection
                .BuildServiceProvider();

        private static async Task Main() => await new Program().StartAsync();

        private async Task StartAsync()
        {
            var discordConfigurator = new DiscordConfigurator(_serviceProvider);

            await discordConfigurator.ConfigureCommandsAsync();

            await discordConfigurator.ConfigureClientEventsAsync();

            await discordConfigurator.InitializeBot();

            await Task.Delay(-1);
        }
    }
}
