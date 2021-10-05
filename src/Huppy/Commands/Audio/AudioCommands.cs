using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
        public AudioCommands(LavaNode lavaNode, AudioService audioService)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
        }

        [Command("Join")]
        public async Task JoinAsync(bool skipConnectedCheck = false)
        {
            if (!skipConnectedCheck && _lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
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
            //Check if bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to any voice channels!");
                return;
            }

            //Handle empty voice room
            var voiceChannel = (Context.User as IVoiceState).VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("Something went wrong with disconnecting");
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
        }

        [Command("Play")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide search terms.");
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
                await ReplyAsync($"I wasn't able to find anything for `{searchQuery}`.");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    foreach (var track in searchResponse.Tracks)
                    {
                        player.Queue.Enqueue(track);
                    }

                    await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} tracks.");
                }
                else
                {
                    var track = searchResponse.Tracks.ToArray()[0];
                    player.Queue.Enqueue(track);
                    await ReplyAsync($"Enqueued: {track.Title}");
                }
            }
            else
            {
                var track = searchResponse.Tracks.ToArray()[0];

                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    var tracks = searchResponse.Tracks.ToArray();

                    for (var i = 0; i < tracks.Length; i++)
                    {
                        if (i == 0)
                        {
                            await player.PlayAsync(track);
                            await ReplyAsync($"Now Playing: {track.Title}");
                        }
                        else
                        {
                            player.Queue.Enqueue(tracks[i]);
                        }
                    }

                    await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} tracks.");
                }
                else
                {
                    await player.PlayAsync(track);
                    await ReplyAsync($"Now Playing: {track.Title}");
                }
            }
        }

        [Command("Resume")]
        [Summary("Resumes the track")]
        public async Task ResumeAsync()
        {
            //Check if bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You have to be in the same room as bot to resume the music");
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
                await ReplyAsync($"Resumed: {player.Track.Title}");
            }
            catch
            {
                await ReplyAsync("Something went wrong");
            }
        }

        [Command("Pause")]
        [Summary("Pauses the track")]
        public async Task PauseAsync()
        {
            //Check if the bot is connected to voice chat
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You have to be in the same room as bot to pause the music");
                return;
            }

            if (player.PlayerState == PlayerState.Paused)
            {
                await ReplyAsync("I cannot pause when I'm not playing anything!");
                return;
            }

            try
            {
                await player.PauseAsync();
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
            //Check if the bot is connected to voice room
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You have to be in the same room as bot to stop the music");
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("Woaaah there, I can't stop the stopped.");
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
            //Check if the user is connected to voice room
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            //Check if the user's room is the same as bot's
            if ((Context.User as IVoiceState).VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You have to be in the same room as bot to skip the music");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't skip when nothing is playing.");
                return;
            }

            if (player.Queue.Count < amount)
            {
                await ReplyAsync("Provide value which is lower than overall count of tracks");
                return;
            }

            /*var voiceChannelUsers = (player.VoiceChannel as SocketVoiceChannel).Users.Where(x => !x.IsBot).ToArray();
            if (MusicService.VoteQueue.Contains(Context.User.Id))
            {
                await ReplyAsync("You can't vote again.");
                return;
            }
            MusicService.VoteQueue.Add(Context.User.Id);
            var percentage = MusicService.VoteQueue.Count / voiceChannelUsers.Length * 100;
            if (percentage < 30)
            {
                await ReplyAsync("You need more than 30% votes to skip this song.");
                return;
            }
            */

            try
            {
                var oldTrack = player.Track;

                if (amount > 0)
                    player.Queue.RemoveRange(0, amount);

                var currenTrack = await player.SkipAsync();

                // var emb = new EmbedBuilder();
                // emb.WithColor(Color.DarkPurple);
                // emb.WithTitle("Now playing");
                // emb.WithDescription(currenTrack.Title);
                // emb.AddField("Duration", currenTrack.Duration.ToString(@"hh\:mm\:ss"));
                // emb.AddField("Author", currenTrack.Author);
                // emb.WithThumbnailUrl(currenTrack.FetchArtworkAsync().GetAwaiter().GetResult());

                // await ReplyAsync($"Skipped: {oldTrack.Title}", false, emb.Build());
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
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I'm not playing any tracks.");
                return;
            }

            var track = player.Track;
            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder
            {
                Title = $"{track.Author} - {track.Title}",
                ThumbnailUrl = artwork,
                Url = track.Url
            }
                .AddField("Id", track.Id)
                .AddField("Duration", track.Duration)
                .AddField("Position", track.Position);

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
