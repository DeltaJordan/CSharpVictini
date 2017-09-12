// <copyright file="Program.cs" company="JordantheBuizel">
// Copyright (c) JordantheBuizel. All rights reserved.
// </copyright>
extern alias http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Commands;
using CSharpDewott.Deserialization;
using CSharpDewott.Encryption;
using CSharpDewott.Logging;
using Discord;
using Discord.WebSocket;
using http::System.Net.Http;
using Newtonsoft.Json;

namespace CSharpDewott
{
    /// <summary>
    /// The main program
    /// </summary>
    internal class Program
    {
        public static readonly Random Random = new Random();
        public static readonly string AppPath = Directory.GetParent(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath).FullName;
        public static Program Instance;
        public HttpClient HttpClient;
        public static Dictionary<ulong, IMessage> LogMessages;
        public static DiscordSocketClient Client;
        public static bool ContinueLock = true;


        // private IAudioClient vClient;

        /// <summary>
        /// Bot's main method
        /// </summary>
        private async Task MainAsync()
        {
            AppDomain.CurrentDomain.ProcessExit += this.CurrentDomain_ProcessExit;

            this.HttpClient = new HttpClient();
            this.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CSharpDewott");

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 50,
                AlwaysDownloadUsers = true,
                LargeThreshold = 500
            });

            Client.Ready += Client_Ready;

            Globals.Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(AppPath, "config.json")));

            await LogHandler.InititializeLogs();

            await Client.LoginAsync(TokenType.Bot, Globals.Settings.Token);

            await Client.StartAsync();

            await CommandHandler.InitializeCommandHandler();

            Instance = this;

            await Task.Delay(-1);
        }

        private async void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            await Client.LogoutAsync();
            // TODO add file logging?
        }

        private static async Task AddLog(ulong channelId)
        {
            using (IAsyncEnumerator<IReadOnlyCollection<IMessage>> enumerator = Client.GetGuild(329174505371074560).GetTextChannel(channelId).GetMessagesAsync(int.MaxValue).GetEnumerator())
            {
                while (await enumerator.MoveNext())
                {
                    foreach (IMessage message in enumerator.Current)
                    {
                        try
                        {
                            LogMessages.Add(message.Id, message);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }

        private static async Task AddReactions()
        {
            foreach (IMessage message in LogMessages.Values)
            {
                if (!message.Content.Contains("gay") || !(message is IUserMessage userMessage))
                {
                    continue;
                }

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

        public static async Task Client_Ready()
        {
            try
            {
                await Client.SetGameAsync("gathering logs, may be slow");

                if (LogMessages == null)
                {
                    LogMessages = new Dictionary<ulong, IMessage>();
                }

                LogMessages.Clear();

                Globals.EncryptKey = Aesgcm.NewKey();

                List<Task> taskList = new List<Task>();

                foreach (SocketTextChannel socketGuildChannel in Client.GetGuild(329174505371074560).TextChannels)
                {
                    taskList.Add(AddLog(socketGuildChannel.Id));
                }

                await Task.WhenAll(taskList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
#if DEBUG
                await Client.SetGameAsync("DEBUG MODE");
#else
                await Client.SetGameAsync("this feels gay");
#endif

                await Task.Run(AddReactions);
            }
        }

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
    }
}
