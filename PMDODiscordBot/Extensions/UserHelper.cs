using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace CSharpDewott.Extensions
{
    public static class UserHelper
    {
        public static string GetUsernameOrNickname(this IUser user)
        {
            if (user is IGuildUser guildUser)
            {
                return string.IsNullOrWhiteSpace(guildUser.Nickname) ? guildUser.Username : guildUser.Nickname;
            }

            return user.Username;
        }
    }
}
