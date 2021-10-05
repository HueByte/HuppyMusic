using System;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Huppy.Services
{
    public class CommandHandlerService : IInjectableSingleton
    {
        private readonly CommandService _commandService;
        private readonly DiscordShardedClient _client;
        private readonly IServiceProvider _serviceProvider;
        public CommandHandlerService(DiscordShardedClient client, CommandService commands, IServiceProvider serviceProvider)
        {
            // DI
            _client = client;
            _commandService = commands;
            _serviceProvider = serviceProvider;

            // events
            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InitializeAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        }

        public async Task HandleCommandAsync(SocketMessage paramMessage)
        {
            if (paramMessage is not SocketUserMessage message) return;

            int argPos = 0;

            if (!(message.HasCharPrefix(';', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var context = new ShardedCommandContext(_client, message);

            await _commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _serviceProvider);

        }

        private async Task LogCommand(SocketCommandContext context, IResult result)
        {
            await Task.Run(() =>
            {
                if (context.Channel is IGuildChannel)
                {
                    var log = $"User: [{context.User.Username}]<->[{context.User.Id}] Discord Server: [{context.Guild.Name}] -> [{context.Message.Content}]";
                    Console.WriteLine(log);
                }
                else
                {
                    var log = $"User: [{context.User.Username}]<->[{context.User.Id}] -> [{context.Message.Content}]";
                    Console.WriteLine(log);
                }
            });
        }
    }
}