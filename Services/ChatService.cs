using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Sevenisko.IceBot.Services
{
    class ChatService
    {
        public async Task ClearMessagesAsync(IGuild guild, IMessageChannel channel, IUser user, int num)
        {
            if (num == 0)
            {
                DiscordReply("Usage: `!clear <amount>`");
                return;
            }
            if (!(await guild.GetUserAsync(user.Id)).GetPermissions(channel as ITextChannel).ManageMessages)
            {
                DiscordReply("You do not have enough permissions to manage messages");
                return;
            }
            
            DiscordReply($"{user.Mention} deleted {num} messages");
        }
    }
}
