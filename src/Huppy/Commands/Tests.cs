using System.Threading.Tasks;
using Discord.Commands;

namespace Huppy.Commands
{
    public class Tests : ModuleBase<SocketCommandContext>
    {
        public Tests() { }

        [Command("Ping")]
        [RequireOwner]
        public async Task Ping()
        {
            await ReplyAsync("Pong");
        }
    }
}