using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Huppy.Responses
{
    public static class DiscordResponse
    {
        public static EmbedBuilder Create(IUser user)
        {
            return new EmbedBuilder().WithAuthor(user.Username, user.GetAvatarUrl())
                                     .WithColor(Color.Teal)
                                     .WithFooter("Post made at ")
                                     .WithCurrentTimestamp();
        }

        public static EmbedBuilder CreateSuccess(IUser user, string successMessage)
        {
            return new EmbedBuilder().WithAuthor(user.Username, user.GetAvatarUrl())
                                     .WithColor(Color.Red)
                                     .WithTitle("Error")
                                     .WithDescription($"```diff\n- {successMessage}\n```")
                                     .WithThumbnailUrl(DiscordEmbedThumbnails.Success)
                                     .WithFooter("Post made at ")
                                     .WithCurrentTimestamp();
        }

        public static EmbedBuilder CreateError(IUser user, string errorMessage)
        {
            return new EmbedBuilder().WithAuthor(user.Username, user.GetAvatarUrl())
                                     .WithColor(Color.Red)
                                     .WithTitle("Error")
                                     .WithDescription("Something went wrong, see detailed error message below")
                                     .AddField("Message", $"```diff\n- {errorMessage}\n```")
                                     .WithThumbnailUrl(DiscordEmbedThumbnails.Error)
                                     .WithFooter("Post made at ")
                                     .WithCurrentTimestamp();
        }

        public static EmbedBuilder CreateMessage(IUser user, string message)
        {
            return new EmbedBuilder().WithAuthor(user.Username, user.GetAvatarUrl())
                                     .WithTitle(message)
                                     .WithFooter("Post made at ")
                                     .WithColor(Color.Teal)
                                     .WithCurrentTimestamp();
        }
    }
}