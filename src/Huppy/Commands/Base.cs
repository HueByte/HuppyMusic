using System.Threading.Tasks;
using Discord.Commands;

namespace Huppy.Commands
{
    public class Base : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;
        public Base(CommandService commands)
        {
            _commandService = commands;
        }

        [Command("Ping")]
        [RequireOwner]
        public async Task Ping()
        {
            await ReplyAsync("Pong");
        }
    }
}