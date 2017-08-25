using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpDewott.Commands;
using CSharpDewott.Deserialization;
using CSharpDewott.Encryption;
using CSharpDewott.Extensions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CSharpDewott.Logging
{
    public static class LogHandler
    {
        private static Dictionary<ulong, IMessage> bufferIMessages = new Dictionary<ulong, IMessage>();

        public static async Task InititializeLogs()
        {
            Program.Client.Log += Log;
            Program.Client.MessageReceived += HandleCommand;
            Program.Client.MessageDeleted += Client_MessageDeleted;
            Program.Client.MessageUpdated += Client_MessageUpdated;
            Program.Client.MessageReceived += Client_AddLogMessage;
            //Program.Client.UserUpdated += Client_UserUpdated;
            //Program.Client.GuildMemberUpdated += Client_GuildMemberUpdated;
        }

        private static async Task Client_GuildMemberUpdated(SocketGuildUser userAfter, SocketGuildUser userBefore)
        {
            if (Program.Client.GetGuild(329174505371074560).Users.FirstOrDefault(e => e.Id == userAfter.Id) != null && !string.IsNullOrWhiteSpace(userAfter.Nickname))
            {
                if (Program.Client.GetGuild(329174505371074560).Roles.Where(e => e.IsMentionable).Any(e => string.Equals(e.Name, userAfter.Nickname, StringComparison.CurrentCultureIgnoreCase)))
                {
                    await userAfter.ModifyAsync(e =>
                    {
                        e.Nickname = string.IsNullOrWhiteSpace(userBefore.Nickname) ? null : userBefore.Nickname;
                    });

                    await (await userAfter.GetOrCreateDMChannelAsync()).SendMessageAsync("You cannot change your nickname to the name of a mentionable role!");
                }
            }
        }

        private static async Task Client_UserUpdated(SocketUser userBefore, SocketUser userAfter)
        {
            if (Program.Client.GetGuild(329174505371074560).Users.FirstOrDefault(e => e.Id == userAfter.Id) != null)
            {
                if (Program.Client.GetGuild(329174505371074560).Roles.Where(e => e.IsMentionable).Any(e => string.Equals(e.Name, userAfter.Username, StringComparison.CurrentCultureIgnoreCase)))
                {
                    await (await userAfter.GetOrCreateDMChannelAsync()).SendMessageAsync("Please change your name to to something that is not a mentionable role as soon as possible.\nIf you don't not agree to this, further actions will be taken against you.");
                    await (await (await Program.Client.GetApplicationInfoAsync()).Owner.GetOrCreateDMChannelAsync()).SendMessageAsync($"{userAfter.Mention} has changed their name to a mentionable role!");
                }
            }
        }

        private static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> messageCache, SocketMessage newMessage, ISocketMessageChannel originChannel)
        {
            //TODO Implement
        }

        private static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> messageCache, ISocketMessageChannel deleteOriginChannel)
        {
            try
            {
                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

                if (Program.Client.GetGuild(329174505371074560).Channels.Any(e => e.Id == deleteOriginChannel.Id) && allCachedMessages.TryGetValue(messageCache.Id, out IMessage matureMessage))
                {
                    IUser deleter = matureMessage.Author;
                    IGuild deleteGuild = Program.Client.GetGuild(329174505371074560);
                    ITextChannel deleteInfoChannel = (ITextChannel)await deleteGuild.GetChannelAsync(335897510813892619);
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithColor(Color.Red);
                    builder.Author = new EmbedAuthorBuilder
                    {
                        Name = deleter.Username,
                        IconUrl = deleter.GetAvatarUrl()
                    };
                    builder.Fields.Add(new EmbedFieldBuilder().WithName("Deleted message content:").WithValue(string.IsNullOrWhiteSpace(matureMessage.Content) ? "empty" : matureMessage.Content));
                    builder.Fields.Add(new EmbedFieldBuilder().WithName("Time:").WithValue(matureMessage.Timestamp));

                    if (matureMessage.Embeds.Count > 0)
                    {
                        builder.Footer = new EmbedFooterBuilder().WithText($"Contains {matureMessage.Embeds.Count} embed(s) that will be appended to the end of this message");
                    }

                    await deleteInfoChannel.SendMessageAsync(string.Empty, false, builder.Build());

                    foreach (IEmbed matureMessageEmbed in matureMessage.Embeds)
                    {
                        await deleteInfoChannel.SendMessageAsync(string.Empty, false, (Embed)matureMessageEmbed);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task Client_AddLogMessage(SocketMessage msg)
        {
            SocketUserMessage message = msg as SocketUserMessage;

            if (message == null)
            {
                return;
            }

            if (bufferIMessages.Count < 25)
            {
                bufferIMessages.Add(message.Id, message);
            }

            if (bufferIMessages.Count < 25)
            {
                return;
            }

            Program.LogMessages.AddRange(bufferIMessages, true);
            bufferIMessages.Clear();
        }

        private static async Task HandleCommand(SocketMessage msg)
        {
            await CommandHandler.PreCommand(msg);
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
