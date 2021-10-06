using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Huppy.Responses;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace Huppy.Services
{
    public class AudioService : IInjectableSingleton
    {
        private readonly DiscordShardedClient _shardedClient;
        private readonly LavaNode _lavaNode;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;
        public AudioService(DiscordShardedClient client, LavaNode lavaNode)
        {
            _shardedClient = client;
            _lavaNode = lavaNode;
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

            // Events
            _lavaNode.OnTrackStarted += OnTrackStarted;
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _lavaNode.OnTrackException += OnTrackException;
            _lavaNode.OnTrackStuck += OnTrackStuck;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosed;
        }

        public async Task OnTrackStarted(TrackStartEventArgs args)
        {
            if (!_disconnectTokens.TryGetValue(args.Player.VoiceChannel.Id, out var value))
            {
                return;
            }

            if (value.IsCancellationRequested)
            {
                return;
            }

            value.Cancel(true);
            await args.Player.TextChannel.SendMessageAsync("Auto disconnect has been cancelled!");
        }

        private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (args.Reason is TrackEndReason.Replaced)
                return;

            var player = args.Player;
            if (!player.Queue.TryDequeue(out var queueable))
            {
                await player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
                _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(10));
                return;
            }

            if (queueable is not LavaTrack track)
            {
                await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            var embed = DiscordResponse.Create(_shardedClient.CurrentUser);

            embed.WithTitle("Success")
                 .WithDescription($"🎵 {args.Reason}: {args.Track.Title}\nNow playing: {track.Title} 🎵")
                 .WithThumbnailUrl(await track.FetchArtworkAsync())
                 .AddField("Duration", $"```{track.Duration}```");


            await args.Player.PlayAsync(track);
            await args.Player.TextChannel.SendMessageAsync(embed: embed.Build());
        }

        private async Task OnTrackException(TrackExceptionEventArgs args)
        {
            args.Player.Queue.Enqueue(args.Track);
            await args.Player.TextChannel?.SendMessageAsync(
                $"{args.Track.Title} has been re-added to queue after throwing an exception.");
        }

        private async Task OnTrackStuck(TrackStuckEventArgs args)
        {
            args.Player.Queue.Enqueue(args.Track);
            await args.Player.TextChannel?.SendMessageAsync(
                $"{args.Track.Title} has been re-added to queue after getting stuck.");
        }

        private async Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            var player = _lavaNode.GetPlayer(_shardedClient.GetGuild(arg.GuildId));
            var voiceChannel = player.VoiceChannel;
            await voiceChannel.DisconnectAsync();
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            await player.TextChannel.SendMessageAsync($"Auto disconnect initiated! Disconnecting in {timeSpan}...");
            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await _lavaNode.LeaveAsync(player.VoiceChannel);
            await player.TextChannel.SendMessageAsync("Invite me again sometime, sugar.");
        }

    }
}