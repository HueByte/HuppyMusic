using System;
using System.Security;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Huppy.Services
{
    public class CommandHandlerService
    {
        private readonly CommandService _commandService;
        private readonly DiscordShardedClient _client;
        private readonly IServiceProvider _serviceProvider;
        public CommandHandlerService(DiscordShardedClient client, CommandService commands, IServiceProvider serviceProvider)
        {
            _client = client;
            _commandService = commands;
            _serviceProvider = serviceProvider;

            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task HandleCommandAsync(SocketMessage paramMessage)
        {
            Console.WriteLine("It worked somehow");
            // Don't process the command if it was a system message
            if (paramMessage is not SocketUserMessage message) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new ShardedCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
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