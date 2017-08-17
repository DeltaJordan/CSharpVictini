using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpDewott.Commands;
using CSharpDewott.Deserialization;
using CSharpDewott.Extensions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CSharpDewott.Logging
{
    public static class LogHandler
    {
        private static Dictionary<ulong, DeserializedMessage> bufferDeserializedMessages = new Dictionary<ulong, DeserializedMessage>();
        private static DiscordSocketClient client;

        public static async Task InititializeLogs()
        {
            client = Program.Client;// Hook the MessageReceived Event into our Command Handler

            client.Log += Log;
            client.MessageReceived += HandleCommand;
            client.MessageDeleted += Client_MessageDeleted;
            client.MessageUpdated += Client_MessageUpdated;
            client.MessageReceived += Client_AddLogMessage;
        }

        private static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> messageCache, SocketMessage newMessage, ISocketMessageChannel originChannel)
        {
            //TODO Implement
        }

        private static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> messageCache, ISocketMessageChannel deleteOriginChannel)
        {
            try
            {
                Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

                foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", ((ITextChannel)deleteOriginChannel).Guild.Name)))
                {
                    allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(
                        File.ReadAllText(file), new JsonSerializerSettings
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            TypeNameHandling = TypeNameHandling.Auto
                        }));
                }

                if (client.GetGuild(329174505371074560).Channels.Any(e => e.Id == deleteOriginChannel.Id) && allCachedMessages.TryGetValue(messageCache.Value.Id, out DeserializedMessage matureMessage))
                {
                    IUser deleter = matureMessage.Author;
                    IGuild deleteGuild = client.GetGuild(329174505371074560);
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


            if (bufferDeserializedMessages.Count < 25)
            {
                bufferDeserializedMessages.Add(message.Id, new DeserializedMessage(message.Id, message.IsTTS, message.IsPinned, message.Content, message.Timestamp, message.EditedTimestamp, message.CreatedAt, new DeserializableUser(message.Author.Id, message.Author.CreatedAt, message.Author.Mention, message.Author.AvatarId, message.Author.DiscriminatorValue, message.Author.Discriminator, message.Author.IsBot, message.Author.IsWebhook, message.Author.Username), new DeserializedChannel(message.Channel.Id, message.Channel.CreatedAt, message.Channel.Name, message.Channel.IsNsfw)));
            }

            if (bufferDeserializedMessages.Count < 25)
            {
                return;
            }

            if (Directory.Exists(Path.Combine(Program.AppPath, "Logs", ((ITextChannel)message.Channel).Guild.Name)))
            {

                Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

                foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", ((ITextChannel)message.Channel).Guild.Name)))
                {
                    allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }

                allCachedMessages = allCachedMessages.AddRange(bufferDeserializedMessages);
                bufferDeserializedMessages.Clear();

                foreach (ITextChannel textChannel in await ((ITextChannel)message.Channel).Guild.GetTextChannelsAsync())
                {
                    File.WriteAllText(Path.Combine(Program.AppPath, "Logs", ((ITextChannel)message.Channel).Guild.Name, $"{textChannel.Id}.json"), JsonConvert.SerializeObject(allCachedMessages.Values.Where(e => e.Channel.Id == textChannel.Id).ToList(), Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }
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
