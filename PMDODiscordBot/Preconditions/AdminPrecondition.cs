using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace CSharpDewott.Preconditions
{
    public class AdminPrecondition : PreconditionAttribute
    {
        public static readonly List<ulong> Whitelist = new List<ulong>
        {
            228019100008316948,
            213135389861478400,
            263205896123842562
        };

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // If this command was executed by that user, return a success
            return Whitelist.Contains(context.User.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be the owner of the bot to run this command.");
        }
    }
}
