using System.Threading.Tasks;
using Discord;

namespace CSharpDewott
{
    class Constants
    {
        private static async Task<IDMChannel> SendDMToJordan(string message)
        {
            IUser jordan = Program.Client.GetUser(228019100008316948);
            IDMChannel jordanDmChannel = await jordan.GetOrCreateDMChannelAsync();
            await jordanDmChannel.SendMessageAsync(message);
            return jordanDmChannel;
        }
    }
}
