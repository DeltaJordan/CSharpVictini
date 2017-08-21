using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace CSharpDewott.Commands.Modules
{
    public class NumberGame
    {
        public ulong ChannelId { get; set; }
        public IUser PlayerOneUser { get; set; }
        public IUser PlayerTwoUser { get; set; }
        public int Difficulty { get; set; }

        public NumberGame(IUser playerOne, IUser playerTwo, int difficulty, CommandContext context)
        {
            this.ChannelId = context.Channel.Id;
            this.PlayerOneUser = playerOne;
            this.PlayerTwoUser = playerTwo;
            this.Difficulty = difficulty;
        }
    }
}
