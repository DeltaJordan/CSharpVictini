// <copyright file="Program.cs" company="JordantheBuizel">
// Copyright (c) JordantheBuizel. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Commands;
using CSharpDewott.Deserialization;
using CSharpDewott.Encryption;
using CSharpDewott.ESixOptions;
using CSharpDewott.Extensions;
using CSharpDewott.GameInfo;
using CSharpDewott.IO;
using CSharpDewott.Logging;
using CSharpDewott.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Image = System.Drawing.Image;

namespace CSharpDewott
{
    /// <summary>
    /// The main program
    /// </summary>
    internal class Program
    {
        public static readonly Random Random = new Random();
        public static readonly string AppPath = Directory.GetParent(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath).FullName;
        public static bool IsNumberGameRunning = false;
        public static IUser PlayingUser;
        public static int CorrectNumber = -1;
        public static int CurrentLevel;
        public static Program Instance;
        public HttpClient HttpClient;
        public static Dictionary<ulong, DeserializedMessage> LogMessages = new Dictionary<ulong, DeserializedMessage>();
        public static DiscordSocketClient Client;


        // private IAudioClient vClient;

        /// <summary>
        /// Bot's main method
        /// </summary>
        public async Task MainAsync()
        {
            AppDomain.CurrentDomain.ProcessExit += this.CurrentDomain_ProcessExit;

            this.HttpClient = new HttpClient();
            this.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MatrixE621");

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 5
            });

            Client.Ready += Client_Ready;

            await LogHandler.InititializeLogs();

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(AppPath, "config.xml"));
            XmlNodeList xmlNodeList = doc.SelectNodes("/Settings/Token");
            if (xmlNodeList != null)
            {
                string token = xmlNodeList[0].InnerText;

                try
                {
                    await Client.LoginAsync(TokenType.Bot, token);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Token invalid!\n\nException: " + exception);
                    throw;
                }
            }
            else
            {
                Console.WriteLine("Invalid config file!");
                Environment.Exit(404);
            }

            await Client.StartAsync();

            await CommandHandler.InitializeCommandHandler();

            Instance = this;

            await Task.Delay(-1);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // TODO add logging?
        }

        public static async Task Client_Ready()
        {
            try
            {
                await Client.SetGameAsync("gathering logs, may be slow");
                foreach (string file in Directory.GetFiles(Path.Combine(AppPath, "Logs", "Jordan's a Fucking Dewott")))
                {
                    if (!Client.GetGuild(329174505371074560).TextChannels.Select(e => e.Id.ToString()).Contains(Path.GetFileNameWithoutExtension(file)))
                    {
                        File.Delete(file);
                    }
                }

                Globals.EncryptKey = Aesgcm.NewKey();

                foreach (SocketTextChannel socketGuildChannel in Client.GetGuild(329174505371074560).TextChannels)
                {
                    FileHelper.CreateIfDoesNotExist(AppPath, "Logs", Client.GetGuild(329174505371074560).Name, new string(socketGuildChannel.Id.ToString().ToCharArray().Where(e => !Path.GetInvalidFileNameChars().Contains(e)).ToArray()) + ".json");

                    List<IReadOnlyCollection<IMessage>> messagesList = await socketGuildChannel.GetMessagesAsync(int.MaxValue).ToList();

                    LogMessages = messagesList.Aggregate(LogMessages, (current, readOnlyCollection) => current.AddRange(readOnlyCollection.Select(e => new DeserializedMessage(e.Id, e.IsTTS, e.IsPinned, e.Content, e.Timestamp, e.EditedTimestamp, e.CreatedAt, new DeserializableUser(e.Author.Id, e.Author.CreatedAt, e.Author.Mention, e.Author.AvatarId, e.Author.DiscriminatorValue, e.Author.Discriminator, e.Author.IsBot, e.Author.IsWebhook, e.Author.Username), new DeserializedChannel(e.Channel.Id, e.Channel.CreatedAt, e.Channel.Name, e.Channel.IsNsfw))).ToDictionary(e => e.Id)));
                }
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
            }
        }

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
    }
}
