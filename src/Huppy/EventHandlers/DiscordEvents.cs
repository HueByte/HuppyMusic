using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Huppy.Services;
using Victoria;

namespace Huppy.EventHandlers
{
    public class DiscordEvents : IInjectableSingleton
    {
        private readonly LavaNode _lavaNode;
        public DiscordEvents(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }
        public async Task OnReadyAsync(DiscordSocketClient _client)
        {
            Console.WriteLine("Sharded client is ready");

            if (!_lavaNode.IsConnected)
            {
                await _lavaNode.ConnectAsync();
                Console.WriteLine("Lavanode connected");
            }
        }
    }
}