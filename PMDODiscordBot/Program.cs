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
using CSharpDewott.Deserialization;
using CSharpDewott.ESixOptions;
using CSharpDewott.GameInfo;
using CSharpDewott.IO;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        public static bool IsNumberGameRunning = false;
        public static IUser PlayingUser;
        public static int CorrectNumber = -1;
        public static int CurrentLevel;
        public static Program Instance;
        public HttpClient HttpClient;

        public static DiscordSocketClient Client;
        private CommandService commands;
        private int currentGuesses;
        private Dictionary<ulong, SocketUserMessage> lastMessages = new Dictionary<ulong, SocketUserMessage>();


        // private IAudioClient vClient;

        /// <summary>
        /// Ordered types of Pokémon, Compliant with PMDO SQL server
        /// </summary>
        public enum PokemonType
        {
            /// <summary>
            /// Type to designate error or no secondary type
            /// </summary>
            None,

            /// <summary>
            /// Bug type
            /// </summary>
            Bug,

            /// <summary>
            /// Dark type
            /// </summary>
            Dark,

            /// <summary>
            /// Dragon type
            /// </summary>
            Dragon,

            /// <summary>
            /// Electric type
            /// </summary>
            Electric,

            /// <summary>
            /// Fairy type
            /// </summary>
            Fairy,

            /// <summary>
            /// Fighting type
            /// </summary>
            Fighting,

            /// <summary>
            /// Fire type
            /// </summary>
            Fire,

            /// <summary>
            /// Flying type
            /// </summary>
            Flying,

            /// <summary>
            /// Ghost type
            /// </summary>
            Ghost,

            /// <summary>
            /// Grass type
            /// </summary>
            Grass,

            /// <summary>
            /// Ground type
            /// </summary>
            Ground,

            /// <summary>
            /// Ice type
            /// </summary>
            Ice,

            /// <summary>
            /// Normal type
            /// </summary>
            Normal,

            /// <summary>
            /// Poison type
            /// </summary>
            Poison,

            /// <summary>
            /// Psychic type
            /// </summary>
            Psychic,

            /// <summary>
            /// Rock type
            /// </summary>
            Rock,

            /// <summary>
            /// Steel type
            /// </summary>
            Steel,

            /// <summary>
            /// Water type
            /// </summary>
            Water
        }

        /// <summary>
        /// Bot's main method
        /// </summary>
        public async Task MainAsync()
        {
            this.HttpClient = new HttpClient();
            this.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MatrixE621");

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                // WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 1000
            });

            this.commands = new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async
            });

            this.commands.Log += Log;
            Client.Log += Log;
            Client.Ready += this.Client_Ready;

            await this.InstallCommands();
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

            Instance = this;

            await Task.Delay(-1);
        }

        public async Task Client_Ready()
        {
            try
            {
                await Client.SetGameAsync("gathering logs, may be slow");

                foreach (SocketTextChannel socketGuildChannel in Client.GetGuild(329174505371074560).TextChannels)
                {
                    FileHelper.CreateIfDoesNotExist(AppPath, "Logs", Client.GetGuild(329174505371074560).Name, new string(socketGuildChannel.Name.ToCharArray().Where(e => !Path.GetInvalidFileNameChars().Contains(e)).ToArray()) + ".xml");

                    List<IReadOnlyCollection<IMessage>> messagesList = await socketGuildChannel.GetMessagesAsync(int.MaxValue).ToList();

                    List<DeserializedMessage> messages = new List<DeserializedMessage>();

                    foreach (IReadOnlyCollection<IMessage> readOnlyCollection in messagesList)
                    {
                        messages.AddRange(readOnlyCollection.Select(e => new DeserializedMessage(e.Id, e.IsTTS, e.IsPinned, e.Content, e.Timestamp, e.EditedTimestamp, e.CreatedAt, new DeserializableUser(e.Author.Id, e.Author.CreatedAt, e.Author.Mention, e.Author.AvatarId, e.Author.DiscriminatorValue, e.Author.Discriminator, e.Author.IsBot, e.Author.IsWebhook, e.Author.Username))));
                    }

                    File.WriteAllText(Path.Combine(AppPath, "Logs", Client.GetGuild(329174505371074560).Name, new string(socketGuildChannel.Name.ToCharArray().Where(e => !Path.GetInvalidFileNameChars().Contains(e)).ToArray()) + ".xml"), JsonConvert.SerializeObject(messages, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }

                await Client.SetGameAsync(".help");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            Client.MessageReceived += this.HandleCommand;
            Client.MessageDeleted += this.Client_MessageDeleted;

            // Discover all of the commands in this assembly and load them.
            await this.commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Globals.CommandService = this.commands;
        }

        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> messageCache, ISocketMessageChannel deleteOriginChannel)
        {
            try
            {
                if (Client.GetGuild(329174505371074560).Channels.Any(e => e.Id == deleteOriginChannel.Id) && this.lastMessages.TryGetValue(messageCache.Value.Id, out SocketUserMessage matureMessage))
                {
                    SocketUser deleter = matureMessage.Author;
                    IGuild deleteGuild = Client.GetGuild(329174505371074560);
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

                    foreach (Embed matureMessageEmbed in matureMessage.Embeds)
                    {
                        await deleteInfoChannel.SendMessageAsync(string.Empty, false, matureMessageEmbed);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task HandleCommand(SocketMessage msg)
        {
            SocketUserMessage message = msg as SocketUserMessage;

            if (message == null || msg.Author.IsBot)
            {
                return;
            }

            if (this.lastMessages.Count == 100)
            {
                ulong keyToDelete = 0;

                foreach (SocketUserMessage cachedMessage in this.lastMessages.Values)
                {
                    if (this.lastMessages.TryGetValue(keyToDelete, out SocketUserMessage mess))
                    {
                        if (cachedMessage.Timestamp.CompareTo(mess.Timestamp) < 0)
                        {
                            keyToDelete = cachedMessage.Id;
                        }
                    }
                    else
                    {
                        keyToDelete = cachedMessage.Id;
                    }
                }

                this.lastMessages.Remove(keyToDelete);
            }

            this.lastMessages.Add(message.Id, message);

            int argPos = 0;

            if (message.Content.StartsWith(".e6 set"))
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(AppPath, "e6options"));

                    UserOptions options = new UserOptions
                    {
                        Id = message.Author.Id,
                        DisplaySources = false,
                        DisplayTags = false,
                        DisplayId = false
                    };

                    if (File.Exists(Path.Combine(AppPath, "e6options", $"{message.Author.Id}.json")))
                    {
                        options = JsonConvert.DeserializeObject<UserOptions>(File.ReadAllText(Path.Combine(AppPath, "e6options", $"{message.Author.Id}.json")));
                    }

                    if (message.Content.Split(' ').Length < 4)
                    {
                        await message.Channel.SendMessageAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]`.\nBy default all options are false.");
                        return;
                    }

                    switch (message.Content.ToLower().Split(' ')[2])
                    {
                        case "tags":
                        case "tag":
                            {
                                if (bool.TryParse(message.Content.Split(' ')[3], out bool result))
                                {
                                    options.DisplayTags = result;
                                }
                                else
                                {
                                    await message.Channel.SendMessageAsync("Option \"Tags\" requires either true or false as the value");
                                    return;
                                }
                            }
                            break;
                        case "source":
                        case "sources":
                            {
                                if (bool.TryParse(message.Content.Split(' ')[3], out bool result))
                                {
                                    options.DisplaySources = result;
                                }
                                else
                                {
                                    await message.Channel.SendMessageAsync("Option \"Sources\" requires either true or false as the value");
                                    return;
                                }
                            }
                            break;
                        case "id":
                            {
                                if (bool.TryParse(message.Content.Split(' ')[3], out bool result))
                                {
                                    options.DisplayId = result;
                                }
                                else
                                {
                                    await message.Channel.SendMessageAsync("Option \"Id\" requires either true or false as the value");
                                    return;
                                }
                            }
                            break;
                        default:
                            {
                                await message.Channel.SendMessageAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]`.\nBy default all options are false.");
                                return;
                            }
                    }

                    File.WriteAllText(Path.Combine(AppPath, "e6options", $"{message.Author.Id}.json"), JsonConvert.SerializeObject(options));

                    await message.Channel.SendMessageAsync("Option set succussfully!");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return;
            }

            if (message.Content.StartsWith(".numbergame stop") && PlayingUser.Id == message.Author.Id)
            {
                await message.Channel.SendMessageAsync("Number Game cancelled");

                CurrentLevel = 0;
                CorrectNumber = 0;
                this.currentGuesses = 0;
                IsNumberGameRunning = false;
                PlayingUser = null;
                return;
            }

            if (message.Content.StartsWith(".numbergame records"))
            {
                List<NGRecords> allRecords = Directory.GetFiles(Path.Combine(AppPath, "numbergame")).Select(file => JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(file))).ToList();

                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = "Number Game Records"
                };

                builder.WithColor(Color.Gold);

                foreach (NGRecords ngRecords in allRecords)
                {
                    switch (ngRecords.Difficulty)
                    {
                        case 1:
                            {
                                builder.Fields.Add(new EmbedFieldBuilder
                                {
                                    IsInline = false,
                                    Name = "Easy",
                                    Value = (await ((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                                });
                            }
                            break;
                        case 2:
                            {
                                builder.Fields.Add(new EmbedFieldBuilder
                                {
                                    IsInline = false,
                                    Name = "Medium",
                                    Value = (await ((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                                });
                            }
                            break;
                        case 3:
                            {
                                builder.Fields.Add(new EmbedFieldBuilder
                                {
                                    IsInline = false,
                                    Name = "Hard",
                                    Value = (await ((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                                });
                            }
                            break;
                        case 4:
                            {
                                builder.Fields.Add(new EmbedFieldBuilder
                                {
                                    IsInline = false,
                                    Name = "Extreme",
                                    Value = (await ((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                                });
                            }
                            break;
                    }
                }

                await message.Channel.SendMessageAsync("<:PogChamp:335551145441492996>", false, builder.Build());

                return;
            }

            if (message.HasCharPrefix('.', ref argPos))
            {
                // Create a Command Context
                CommandContext context = new CommandContext(Client, message);

                string commandName = message.Content.Substring(1).Split(' ')[0].Trim();

                if (!Commands.Commands.Whitelist.Contains(message.Author.Id) && !message.Author.IsBot)
                {

                    if (File.Exists(Path.Combine(Program.AppPath, "blacklists", $"{commandName}.txt")))
                    {
                        if (File.ReadAllLines(Path.Combine(Program.AppPath, "blacklists", $"{commandName}.txt")).Any(e => e.Contains(message.Channel.Id.ToString())))
                        {
                            await message.Channel.SendMessageAsync("This command has been blacklisted from this channel");
                            return;
                        }
                    }

                    if (File.ReadAllLines(Path.Combine(Program.AppPath, "blacklists", "all.txt")).Any(e => e.Contains(message.Channel.Id.ToString())))
                    {
                        await message.Channel.SendMessageAsync("All commands have been blacklisted from this channel");
                        return;
                    }
                }

                // Execute the command. (result does not indicate a return value,
                // rather an object stating if the command executed succesfully)
                IResult result = await this.commands.ExecuteAsync(context, argPos);
                if (!result.IsSuccess)
                {
                    if (result.Error != CommandError.UnknownCommand)
                    {
                        await message.Channel.SendMessageAsync($"{result.ErrorReason}");
                    }

                    Console.Out.WriteLine($"[HandleCommand] {result.ErrorReason}");
                }

                return;
            }

            int luckyNumber = Globals.Random.Next(0, 1000);

            if (luckyNumber == 166)
            {
                await message.Channel.SendMessageAsync("This feels gay");
            }

            if (IsNumberGameRunning && msg.Author.Id == PlayingUser.Id &&
                msg.Channel.Name.ToLower().Contains("bot"))
            {
                if (int.TryParse(msg.Content, out int guess))
                {
                    if (guess > CorrectNumber)
                    {
                        this.currentGuesses++;
                        await msg.Channel.SendMessageAsync($"Too high! You have guessed {this.currentGuesses} times.");
                    }

                    if (guess < CorrectNumber)
                    {
                        this.currentGuesses++;
                        await msg.Channel.SendMessageAsync($"Too low! You have guessed {this.currentGuesses} times.");
                    }

                    if (guess == CorrectNumber)
                    {
                        this.currentGuesses++;
                        await msg.Channel.SendMessageAsync(
                            $"Congrats! You have guessed {this.currentGuesses} times to get the correct number, {CorrectNumber}.");

                        if (!File.Exists(Path.Combine(AppPath, "numbergame", $"record{CurrentLevel}.json")))
                        {
                            FileHelper.CreateIfDoesNotExist(AppPath, "numbergame", $"record{CurrentLevel}.json");

                            NGRecords records = new NGRecords
                            {
                                Difficulty = CurrentLevel,
                                Guesses = this.currentGuesses,
                                Id = message.Author.Id
                            };

                            string json = JsonConvert.SerializeObject(records);

                            File.WriteAllText(Path.Combine(AppPath, "numbergame", $"record{CurrentLevel}.json"), json);
                        }
                        else
                        {
                            NGRecords records = new NGRecords
                            {
                                Difficulty = CurrentLevel,
                                Guesses = this.currentGuesses,
                                Id = message.Author.Id
                            };

                            NGRecords oldRecords = JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(Path.Combine(AppPath, "numbergame", $"record{CurrentLevel}.json")));

                            if (oldRecords.Guesses > this.currentGuesses)
                            {
                                string json = JsonConvert.SerializeObject(records);

                                File.WriteAllText(Path.Combine(AppPath, "numbergame", $"record{CurrentLevel}.json"), json);
                            }
                        }

                        NGRecords newestRecords = JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(Path.Combine(AppPath, "numbergame", $"record{CurrentLevel}.json")));

                        await msg.Channel.SendMessageAsync($"The current record is {newestRecords.Guesses}, set by {((ITextChannel)message.Channel).Guild.GetUserAsync(newestRecords.Id).Result.Username}");

                        CurrentLevel = 0;
                        CorrectNumber = 0;
                        this.currentGuesses = 0;
                        IsNumberGameRunning = false;
                        PlayingUser = null;
                    }
                }
            }

            if (msg.Content.ToLower().Contains("hello") && msg.Content.ToLower().Contains("csharpdewott"))
            {
                await msg.Channel.SendMessageAsync($"Hello {msg.Author.Username}!");
            }

            if (msg.Content.Contains("(╯°□°）╯︵ ┻━┻"))
            {
                await msg.Channel.SendMessageAsync("┬─┬﻿ ノ( ゜-゜ノ)");
            }

            if (msg.Content.Contains("┬─┬﻿ ノ( ゜-゜ノ)"))
            {
                await msg.Channel.SendMessageAsync("https://youtu.be/To6nhootM3w");
            }

            if (msg.Content.ToLower().Contains("no u") && Globals.Random.NextDouble() < 0.3)
            {
                await msg.Channel.SendMessageAsync("no u");
            }
        }

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
