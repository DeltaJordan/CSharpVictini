using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Cleverbot.Net;
using CSharpDewott.Commands;
using CSharpDewott.Deserialization;
using CSharpDewott.Encryption;
using CSharpDewott.Extensions;
using Discord;
using Discord.WebSocket;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Color = System.Drawing.Color;

namespace CSharpDewott.Logging
{
    public static class LogHandler
    {
        public static List<(string Title, string Url)> JonVideoList = new List<(string Title, string Url)>();

        private static readonly string[] CommonWords =
        {
            "the",
            "of",
            "and",
            "to",
            "a",
            "in",
            "for",
            "is",
            "on",
            "that",
            "by",
            "this",
            "with",
            "i",
            "you",
            "it",
            "not",
            "or",
            "be",
            "are",
            "from",
            "at",
            "as",
            "your",
            "all",
            "have",
            "new",
            "more",
            "an",
            "was",
            "we",
            "will",
            "can",
            "us",
            "about",
            "if",
            "my",
            "has",
            "but",
            "our",
            "one",
            "other",
            "do",
            "no",
            "what"
        };

        public static async Task InititializeLogs()
        {
            Program.Client.Log += Log;
            Program.Client.MessageReceived += HandleCommand;
            Program.Client.MessageDeleted += Client_MessageDeleted;
            Program.Client.MessageUpdated += Client_MessageUpdated;
            Program.Client.MessageReceived += Client_AddLogMessage;
            Program.Client.MessageReceived += CleverBotHook;
            Program.Client.MessageReceived += JonTron_Hook;
            //Program.Client.UserUpdated += Client_UserUpdated;
            //Program.Client.GuildMemberUpdated += Client_GuildMemberUpdated;

            // Create the service.
            YouTubeService service = new YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = "CSharpDewott",
                ApiKey = "AIzaSyAY3o5zW5uYpi5Z3EjBTDXcl79q5t_01Ag",
            });

            string nextPageToken = string.Empty;

            while (nextPageToken != null)
            {
                PlaylistItemsResource.ListRequest playlistItemRequest = service.PlaylistItems.List("snippet");
                playlistItemRequest.PlaylistId = "PLuqtmDTb02uG5DCgZP9s5d94QY68V2k1Z";
                playlistItemRequest.MaxResults = 50;
                playlistItemRequest.PageToken = nextPageToken;

                PlaylistItemListResponse playlistItemListResponse = await playlistItemRequest.ExecuteAsync();

                JonVideoList.AddRange(playlistItemListResponse.Items.Select(playlistItem => (playlistItem.Snippet.Title, "https://youtu.be/" + playlistItem.Snippet.ResourceId.VideoId)));

                nextPageToken = playlistItemListResponse.NextPageToken;
            }
        }

        private static async Task JonTron_Hook(SocketMessage msg)
        {
            if (msg.Content == string.Empty || msg.Author.IsBot || msg.Content.Split(' ').Any(e => CommonWords.Contains(e.ToLower())) || Globals.Random.Next(1, 1000) > 50)
            {
                return;
            }
            
            List<(string Title, string Url)> selectedVideos = JonVideoList.Where(e => e.Title.Split(' ').Any(f => msg.Content.Split(' ').Any(g => string.Equals(f, g, StringComparison.CurrentCultureIgnoreCase)))).ToList();

            if (selectedVideos.Any())
            {
                await msg.Channel.SendMessageAsync(selectedVideos[Globals.Random.Next(0, selectedVideos.Count - 1)].Url);
            }
        }

        private static async Task CleverBotHook(SocketMessage msg)
        {
//            if (msg.Channel.Id != 354054956040454144)
//            {
//                return;
//            }
//
//            CleverbotSession session = await CleverbotSession.NewSessionAsync(Globals.Settings.CleverBotUser, Globals.Settings.CleverBotKey);
//
//            await msg.Channel.SendMessageAsync(await session.SendAsync(msg.Content));
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

        private static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> messageCache, SocketMessage newMessage, ISocketMessageChannel editOriginChannel)
        {
            try
            {
                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

                if (editOriginChannel.Id == 335897510813892619)
                {
                    return;
                }

                if (Program.Client.GetGuild(329174505371074560).Channels.Any(e => e.Id == editOriginChannel.Id) && allCachedMessages.TryGetValue(messageCache.Id, out IMessage logMessage))
                {
                    IUser editor = logMessage.Author;
                    IGuild editGuild = Program.Client.GetGuild(329174505371074560);
                    ITextChannel logChannel = (ITextChannel)await editGuild.GetChannelAsync(335897510813892619);
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithColor(Color.Yellow);
                    builder.Author = new EmbedAuthorBuilder
                    {
                        Name = editor.Username,
                        IconUrl = editor.GetAvatarUrl()
                    };
                    builder.Fields.Add(new EmbedFieldBuilder().WithName("Original message content:").WithValue(string.IsNullOrWhiteSpace(logMessage.Content) ? "empty" : logMessage.Content));
                    builder.Fields.Add(new EmbedFieldBuilder().WithName("New message content:").WithValue(string.IsNullOrWhiteSpace(newMessage.Content) ? "empty" : newMessage.Content));
                    builder.Fields.Add(new EmbedFieldBuilder().WithName("Time:").WithValue(newMessage.EditedTimestamp));

                    if (logMessage.Embeds.Count > 0)
                    {
                        builder.Footer = new EmbedFooterBuilder().WithText($"Contains {logMessage.Embeds.Count} embed(s) that will be appended to the end of this message");
                    }

                    await logChannel.SendMessageAsync(string.Empty, false, builder.Build());

                    foreach (IEmbed matureMessageEmbed in logMessage.Embeds)
                    {
                        await logChannel.SendMessageAsync(string.Empty, false, (Embed)matureMessageEmbed);
                    }

                    if (logMessage.Content.Contains("gay") && logMessage is IUserMessage userMessage)
                    {
                        if (userMessage.Reactions.TryGetValue(new Emoji("🏳️‍🌈"), out ReactionMetadata value))
                        {
                            if (!value.IsMe)
                            {
                                await userMessage.AddReactionAsync(new Emoji("🏳️‍🌈"));
                            }
                        }
                        else
                        {
                            await userMessage.AddReactionAsync(new Emoji("🏳️‍🌈"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> messageCache, ISocketMessageChannel deleteOriginChannel)
        {
            try
            {
                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

                if (deleteOriginChannel.Id == 335897510813892619)
                {
                    return;
                }

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

                    Program.LogMessages.Remove(messageCache.Id);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task Client_AddLogMessage(SocketMessage msg)
        {
            Program.LogMessages.Add(msg.Id, msg);

            if (msg.Content.ToLower().Contains("gay") && msg is IUserMessage userMessage)
            {
                await userMessage.AddReactionAsync(new Emoji("🏳️‍🌈"));
            }
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
