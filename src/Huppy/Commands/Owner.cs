using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Huppy.Responses;

namespace Huppy.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
        public Owner() { }

        [Command("Status")]
        [RequireOwner]
        public async Task ChangeStatus([Remainder] string status)
        {
            await Context.Client.SetGameAsync(status, null, ActivityType.Playing);
            await Context.Message.DeleteAsync();

            var embed = DiscordResponse.CreateMessage(Context.Client.CurrentUser, $"New Status: ```{status}```");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("EmbedMessage")]
        [RequireOwner]
        public async Task EmbedMessage([Remainder] string message)
        {
            await Context.Message.DeleteAsync();
            var embed = DiscordResponse.CreateMessage(Context.Client.CurrentUser, message);
            await ReplyAsync(embed: embed.Build());
        }
    }
}