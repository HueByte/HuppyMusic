using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Huppy.Responses;
using Huppy.Services;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace Huppy.Commands.Audio
{
    public class AudioCommands : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;
        private readonly DiscordShardedClient _shardedClient;
        public AudioCommands(LavaNode lavaNode, AudioService audioService, DiscordShardedClient shardedClient)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
            _shardedClient = shardedClient;
        }

        [Command("Join")]
        public async Task JoinAsync(bool skipConnectedCheck = false)
        {
            EmbedBuilder embed;
            if (!skipConnectedCheck && _lavaNode.HasPlayer(Context.Guild))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm already connected to a voice channel!");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "You must be connected to a voice channel!");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Leave")]
        [Summary("Makes the bot Leave the voice chat")]
        public async Task LeaveAsync()
        {
            EmbedBuilder embed;

            //Check if bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to any voice channels");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            //Handle empty voice room
            var voiceChannel = (Context.User as IVoiceState).VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "Something went wrong with disconnecting");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            //Try to leave the voice room
            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("ForceLeave")]
        public async Task ForceLeaveAsync()
        {
            var player = _lavaNode.GetPlayer(_shardedClient.GetGuild(Context.Guild.Id));
            var voiceChannel = player.VoiceChannel;
            await voiceChannel.DisconnectAsync();
        }

        [Command("Play")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            EmbedBuilder embed;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "Please provide search terms");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await JoinAsync(true);
            }

            var searchResponse = await _lavaNode.SearchAsync(SearchType.Direct, searchQuery);
            if (searchResponse.Status == SearchStatus.LoadFailed ||
                searchResponse.Status == SearchStatus.NoMatches)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, $"I wasn't able to find anything for `{searchQuery}`.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            embed = DiscordResponse.Create(Context.Client.CurrentUser);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    foreach (var track in searchResponse.Tracks)
                    {
                        player.Queue.Enqueue(track);
                    }

                    embed.WithTitle("Success")
                         .WithDescription($"🎵 Enqueued {searchResponse.Tracks.Count} tracks. 🎵")
                         .WithThumbnailUrl(DiscordEmbedThumbnails.Success);

                    await ReplyAsync(embed: embed.Build());
                }
                else
                {
                    var track = searchResponse.Tracks.ToArray()[0];
                    player.Queue.Enqueue(track);

                    embed.WithTitle("Success")
                         .WithDescription($"🎵 Enqueued: {track.Title} 🎵")
                         .WithThumbnailUrl(DiscordEmbedThumbnails.Success);

                    await ReplyAsync(embed: embed.Build());

                }
            }
            else
            {
                var track = searchResponse.Tracks.ToArray()[0];
                string artwork = await track.FetchArtworkAsync();

                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    var tracks = searchResponse.Tracks.ToArray();

                    for (var i = 0; i < tracks.Length; i++)
                    {
                        if (i == 0)
                        {

                            embed.WithTitle("Success")
                                 .WithDescription($"🎵 Now Playing: {track.Title} 🎵")
                                 .WithThumbnailUrl(artwork);

                            await player.PlayAsync(track);
                            await ReplyAsync(embed: embed.Build());

                        }
                        else
                        {
                            player.Queue.Enqueue(tracks[i]);
                        }
                    }

                    embed.WithTitle("Success")
                         .WithDescription($"🎵 Enqueued {searchResponse.Tracks.Count} tracks. 🎵")
                         .WithThumbnailUrl(artwork);

                    await ReplyAsync(embed: embed.Build());
                }
                else
                {
                    embed.WithTitle("Success")
                         .WithDescription($"🎵 Now Playing: {track.Title} 🎵")
                         .WithThumbnailUrl(await track.FetchArtworkAsync())
                         .AddField("Duration", $"```{track.Duration}```");

                    await player.PlayAsync(track);
                    await ReplyAsync(embed: embed.Build());

                }
            }
        }

        [Command("queue")]
        public async Task QueueAsync()
        {
            EmbedBuilder embed;

            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to a voice channel.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            var queue = player.Queue.ToArray();

            embed = DiscordResponse.Create(Context.Client.CurrentUser);
            embed.WithTitle("Your queue");
            embed.WithDescription("Your next 10 songs in queue");

            int takeTenOrLess = player.Queue.Count > 10 ? 10 : player.Queue.Count;
            int current = 1;
            while (current <= takeTenOrLess)
            {
                embed.AddField($"🎵 **{current}** ", $"```{queue[current - 1].Title}```", true);
                current++;
            }

            await ReplyAsync(embed: embed.Build());

        }

        [Command("Resume")]
        [Summary("Resumes the track")]
        public async Task ResumeAsync()
        {
            EmbedBuilder embed;

            //Check if bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to a voice channel.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "You have to be in the same room as bot to resume the music");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                await ReplyAsync("But I am playing!");
                return;
            }
            try
            {
                await player.ResumeAsync();
                // await ReplyAsync($"Resumed: {player.Track.Title}");
            }
            catch (Exception e)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "Something went wrong");
                await ReplyAsync(embed: embed.Build());
                return;
            }
        }

        [Command("Pause")]
        [Summary("Pauses the track")]
        public async Task PauseAsync()
        {
            EmbedBuilder embed;

            //Check if the bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to a voice channel.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "You have to be in the same room as bot to pause the music");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            if (player.PlayerState == PlayerState.Paused)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I cannot pause when I'm not playing anything!");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            try
            {
                await player.PauseAsync();

                embed = DiscordResponse.Create(Context.Client.CurrentUser);
                embed.WithColor(Color.Gold);
                embed.WithDescription($"Paused: {player.Track.Title}");
                embed.WithThumbnailUrl(await player.Track.FetchArtworkAsync());

                await ReplyAsync($"Paused: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Stop")]
        [Summary("Stops the music")]
        public async Task StopAsync()
        {
            EmbedBuilder embed;

            //Check if the bot is connected to voice room
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to a voice channel.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "You have to be in the same room as bot to stop the music");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("Woaaah there, I can't stop the what's already stopped.");
                return;
            }

            try
            {
                await player.StopAsync();
                player.Queue.Clear();
                await ReplyAsync("No longer playing anything.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Skip")]
        [Alias("s")]
        [Summary("Skips the track or entered amount of tracks\nUsage: $Skip 10")]
        public async Task SkipAsync(int amount = 0)
        {
            EmbedBuilder embed;

            //Check if the user is connected to voice room
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to a voice channel.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "You have to be in the same room as bot to skip the music");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't skip when nothing is playing.");
                return;
            }

            if (player.Queue.Count < amount)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "Provide value which is lower than overall count of tracks");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            try
            {
                var oldTrack = player.Track;

                if (amount > 0)
                    player.Queue.RemoveRange(0, amount);

                var currenTrack = await player.SkipAsync();

                embed = DiscordResponse.Create(Context.Client.CurrentUser);
                embed.WithTitle($"{currenTrack.Current.Author} - {currenTrack.Current.Title}")
                     .WithThumbnailUrl(await currenTrack.Current.FetchArtworkAsync())
                     .WithUrl(currenTrack.Current.Url)
                     .AddField("Id", $"```{currenTrack.Current.Id}```")
                     .AddField("Duration", $"```{currenTrack.Current.Duration}```");

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        //To Update
        [Command("Seek")]
        [Summary("Check thes status")]
        public async Task SeekAsync(TimeSpan timeSpan)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't seek when nothing is playing.");
                return;
            }

            try
            {
                await player.SeekAsync(timeSpan);
                await ReplyAsync($"I've seeked `{player.Track.Title}` to {timeSpan}.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Volume")]
        [Summary("Change the volume with max of 100\nUsage: $Volume <number>")]
        public async Task VolumeAsync(ushort volume)
        {
            //Check if the USER IS OWNER OF THE BOT TO NOT LET THEM EARRAPE 
            if (volume > 100)
            {
                if (Context.User.Id != 215556401467097088)
                {
                    await ReplyAsync("You can't make it higher than 100");
                    return;
                }
            }

            //Check if the bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You have to be in the same room as bot to change the volume");
                return;
            }

            try
            {
                await player.UpdateVolumeAsync(volume);
                await ReplyAsync($"I've changed the player volume to {volume}.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("NowPlaying"), Alias("Np")]
        [Summary("Get current track")]
        public async Task NowPlayingAsync()
        {
            EmbedBuilder embed;

            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not connected to a voice channel.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                embed = DiscordResponse.CreateError(Context.Client.CurrentUser, "I'm not playing any tracks.");
                await ReplyAsync(embed: embed.Build());
                return;
            }

            var track = player.Track;

            embed = DiscordResponse.Create(Context.Client.CurrentUser);
            embed.WithTitle($"{track.Author} - {track.Title}")
                 .WithThumbnailUrl(await track.FetchArtworkAsync())
                 .WithUrl(track.Url)
                 .AddField("Id", $"```{track.Id}```")
                 .AddField("Duration", $"```{track.Duration}```")
                 .AddField("Position", $"```{track.Position}```");

            await ReplyAsync(embed: embed.Build());
        }

        //     [Command("Genius", RunMode = RunMode.Async)]
        //     [Summary("Get Genius lyrics")]
        //     public async Task ShowGeniusLyrics()
        //     {
        //         if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        //         {
        //             await ReplyAsync("I'm not connected to a voice channel.");
        //             return;
        //         }

        //         if (player.PlayerState != PlayerState.Playing)
        //         {
        //             await ReplyAsync("Woaaah there, I'm not playing any tracks.");
        //             return;
        //         }

        //         var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
        //         if (string.IsNullOrWhiteSpace(lyrics))
        //         {
        //             await ReplyAsync($"No lyrics found for {player.Track.Title}");
        //             return;
        //         }

        //         var splitLyrics = lyrics.Split('\n');
        //         var stringBuilder = new StringBuilder();
        //         foreach (var line in splitLyrics)
        //         {
        //             if (Range.Contains(stringBuilder.Length))
        //             {
        //                 await ReplyAsync($"```{stringBuilder}```");
        //                 stringBuilder.Clear();
        //             }
        //             else
        //             {
        //                 stringBuilder.AppendLine(line);
        //             }
        //         }

        //         await ReplyAsync($"```{stringBuilder}```");
        //     }

        //     [Command("OVH", RunMode = RunMode.Async)]
        //     [Summary("Get OVH lyrics")]
        //     public async Task ShowOvhLyrics()
        //     {
        //         if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        //         {
        //             await ReplyAsync("I'm not connected to a voice channel.");
        //             return;
        //         }

        //         if (player.PlayerState != PlayerState.Playing)
        //         {
        //             await ReplyAsync("Woaaah there, I'm not playing any tracks.");
        //             return;
        //         }

        //         var lyrics = await player.Track.FetchLyricsFromOvhAsync();
        //         if (string.IsNullOrWhiteSpace(lyrics))
        //         {
        //             await ReplyAsync($"No lyrics found for {player.Track.Title}");
        //             return;
        //         }

        //         var splitLyrics = lyrics.Split('\n');
        //         var stringBuilder = new StringBuilder();
        //         foreach (var line in splitLyrics)
        //         {
        //             if (Range.Contains(stringBuilder.Length))
        //             {
        //                 await ReplyAsync($"```{stringBuilder}```");
        //                 stringBuilder.Clear();
        //             }
        //             else
        //             {
        //                 stringBuilder.AppendLine(line);
        //             }
        //         }

        //         await ReplyAsync($"```{stringBuilder}```");
        //     }
    }
}
