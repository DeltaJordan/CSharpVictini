using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;

namespace CSharpDewott.Commands
{
    public static class CommandHandler
    {
        private static int currentGuesses;
        private static DiscordSocketClient client;
        private static CommandService commands;

        public static async Task InitializeCommandHandler()
        {
            commands = new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async
            });

            commands.Log += LogHandler.Log;

            client = Program.Client;
            Globals.CommandService = commands;
            
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public static async Task PreCommand(SocketMessage msg)
        {
            SocketUserMessage message = msg as SocketUserMessage;

            if (message == null || msg.Author.IsBot)
            {
                return;
            }

            // Add string testing after the argPos
            int argPos = 0;

            if (message.Content.StartsWith("*") & message.Content.EndsWith("*") || message.Content.StartsWith("_") & message.Content.EndsWith("_"))
            {
                string potentialRequest = message.Content.Replace("*", string.Empty).Replace("_", string.Empty).ToLower();

                Image image = null;

                if (HttpHelper.UrlExists($"https://play.pokemonshowdown.com/sprites/xyani/{potentialRequest}.gif"))
                {
                    byte[] imageBytes = await Program.Instance.HttpClient.GetByteArrayAsync($"https://play.pokemonshowdown.com/sprites/xyani/{potentialRequest}.gif");

                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        image = Image.FromStream(ms);
                    }
                }

                if (image != null)
                {
                    if (File.Exists(Path.Combine(Program.AppPath, "pokemon.gif")))
                    {
                        File.Delete(Path.Combine(Program.AppPath, "pokemon.gif"));
                    }

                    image.Save(Path.Combine(Program.AppPath, "pokemon.gif"));

                    await message.Channel.SendFileAsync(Path.Combine(Program.AppPath, "pokemon.gif"));
                }
            }

            if (message.Content.StartsWith(".e6 set"))
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(Program.AppPath, "e6options"));

                    UserOptions options = new UserOptions
                    {
                        Id = message.Author.Id,
                        BlackList = new List<string>
                        {
                            "scat",
                            "gore"
                        },
                        DisplaySources = false,
                        DisplayTags = false,
                        DisplayId = false
                    };

                    if (File.Exists(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json")))
                    {
                        options = JsonConvert.DeserializeObject<UserOptions>(File.ReadAllText(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json")));
                    }

                    if (message.Content.Split(' ').Length < 4)
                    {
                        await message.Channel.SendMessageAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]` or for blacklists `.e6 set [blacklist add|exclude|blacklist remove|include] [tag]`.\nBy default all options are false and blacklist contains \"scat\" and \"gore\".");
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
                        case "exclude":
                        {
                            options.BlackList.Add(message.Content.Split(' ')[3]);

                            await message.Channel.SendMessageAsync($"Option set succussfully! Your blacklisted tags are now {string.Join(", ", options.BlackList)}.");
                            File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json"), JsonConvert.SerializeObject(options));
                            return;
                        }
                        case "include":
                        {
                            options.BlackList.RemoveAll(e => string.Equals(e, message.Content.Split(' ')[3], StringComparison.CurrentCultureIgnoreCase));

                            await message.Channel.SendMessageAsync($"Option set succussfully! Your blacklisted tags are now {string.Join(", ", options.BlackList)}.");
                            File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json"), JsonConvert.SerializeObject(options));
                            return;
                        }
                        case "blacklist":
                        {
                            string[] commandArgs = message.Content.ToLower().Split(' ');
                            if (commandArgs.Length < 5)
                            {
                                await message.Channel.SendMessageAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]` or for blacklists `.e6 set [blacklist add|exclude|blacklist remove|include] [tag].\nBy default all options are false and blacklist contains \"scat\" and \"gore\".");
                                return;
                            }

                            switch (commandArgs[3])
                            {
                                case "set":
                                {
                                    options.BlackList.Add(commandArgs[4]);

                                    await message.Channel.SendMessageAsync($"Option set succussfully! Your blacklisted tags are now {string.Join(", ", options.BlackList)}.");
                                    File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json"), JsonConvert.SerializeObject(options));
                                    return;
                                }
                                case "remove":
                                {
                                    options.BlackList.RemoveAll(e => string.Equals(e, commandArgs[4], StringComparison.CurrentCultureIgnoreCase));

                                    await message.Channel.SendMessageAsync($"Option set succussfully! Your blacklisted tags are now {string.Join(", ", options.BlackList)}.");
                                    File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json"), JsonConvert.SerializeObject(options));
                                    return;
                                }
                                default:
                                {
                                    await message.Channel.SendMessageAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]` or for blacklists `.e6 set [blacklist add|exclude|blacklist remove|include] [tag].\nBy default all options are false and blacklist contains \"scat\" and \"gore\".");
                                    return;
                                }
                            }
                        }
                        default:
                        {
                            await message.Channel.SendMessageAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]` or for blacklists `.e6 set [blacklist add|exclude|blacklist remove|include] [tag].\nBy default all options are false and blacklist contains \"scat\" and \"gore\".");
                            return;
                        }
                    }

                    File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{message.Author.Id}.json"), JsonConvert.SerializeObject(options));

                    await message.Channel.SendMessageAsync("Option set succussfully!");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return;
            }

            if (message.Content.StartsWith(".numbergame stop") && Program.PlayingUser.Id == message.Author.Id)
            {
                await message.Channel.SendMessageAsync("Number Game cancelled");

                Program.CurrentLevel = 0;
                Program.CorrectNumber = 0;
                currentGuesses = 0;
                Program.IsNumberGameRunning = false;
                Program.PlayingUser = null;
                return;
            }

            if (message.Content.StartsWith(".numbergame records"))
            {
                List<NGRecords> allRecords = Directory.GetFiles(Path.Combine(Program.AppPath, "numbergame")).Select(file => JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(file))).ToList();

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
                                Value = (await((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                            });
                        }
                            break;
                        case 2:
                        {
                            builder.Fields.Add(new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Medium",
                                Value = (await((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                            });
                        }
                            break;
                        case 3:
                        {
                            builder.Fields.Add(new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Hard",
                                Value = (await((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                            });
                        }
                            break;
                        case 4:
                        {
                            builder.Fields.Add(new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Extreme",
                                Value = (await((ITextChannel)message.Channel).Guild.GetUserAsync(ngRecords.Id)).Username + $" : {ngRecords.Guesses}"
                            });
                        }
                            break;
                    }
                }

                await message.Channel.SendMessageAsync("<:PogChamp:335551145441492996>", false, builder.Build());

                return;
            }


            await HandleCommand(message);
        }

        public static async Task HandleCommand(SocketUserMessage message)
        {
            int argPos = 0;

            if (message.HasCharPrefix('.', ref argPos))
            {
                // Create a Command Context
                CommandContext context = new CommandContext(client, message);

                string commandName = message.Content.Substring(1).Split(' ')[0].Trim();

                if (!AdminPrecondition.Whitelist.Contains(message.Author.Id) && !message.Author.IsBot)
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
                IResult result = await commands.ExecuteAsync(context, argPos);
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

            await PostCommand(message);
        }

        public static async Task PostCommand(SocketUserMessage message)
        {
            int luckyNumber = Globals.Random.Next(0, 1000);

            if (luckyNumber == 166)
            {
                await message.Channel.SendMessageAsync("This feels gay");
            }

            if (Program.IsNumberGameRunning && message.Author.Id == Program.PlayingUser.Id &&
                message.Channel.Name.ToLower().Contains("bot"))
            {
                if (int.TryParse(message.Content, out int guess))
                {
                    if (guess > Program.CorrectNumber)
                    {
                        currentGuesses++;
                        await message.Channel.SendMessageAsync($"Too high! You have guessed {currentGuesses} times.");
                    }

                    if (guess < Program.CorrectNumber)
                    {
                        currentGuesses++;
                        await message.Channel.SendMessageAsync($"Too low! You have guessed {currentGuesses} times.");
                    }

                    if (guess == Program.CorrectNumber)
                    {
                        currentGuesses++;
                        await message.Channel.SendMessageAsync(
                            $"Congrats! You have guessed {currentGuesses} times to get the correct number, {Program.CorrectNumber}.");

                        if (!File.Exists(Path.Combine(Program.AppPath, "numbergame", $"record{Program.CurrentLevel}.json")))
                        {
                            FileHelper.CreateIfDoesNotExist(Program.AppPath, "numbergame", $"record{Program.CurrentLevel}.json");

                            NGRecords records = new NGRecords
                            {
                                Difficulty = Program.CurrentLevel,
                                Guesses = currentGuesses,
                                Id = message.Author.Id
                            };

                            string json = JsonConvert.SerializeObject(records);

                            File.WriteAllText(Path.Combine(Program.AppPath, "numbergame", $"record{Program.CurrentLevel}.json"), json);
                        }
                        else
                        {
                            NGRecords records = new NGRecords
                            {
                                Difficulty = Program.CurrentLevel,
                                Guesses = currentGuesses,
                                Id = message.Author.Id
                            };

                            NGRecords oldRecords = JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(Path.Combine(Program.AppPath, "numbergame", $"record{Program.CurrentLevel}.json")));

                            if (oldRecords.Guesses > currentGuesses)
                            {
                                string json = JsonConvert.SerializeObject(records);

                                File.WriteAllText(Path.Combine(Program.AppPath, "numbergame", $"record{Program.CurrentLevel}.json"), json);
                            }
                        }

                        NGRecords newestRecords = JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(Path.Combine(Program.AppPath, "numbergame", $"record{Program.CurrentLevel}.json")));

                        await message.Channel.SendMessageAsync($"The current record is {newestRecords.Guesses}, set by {((ITextChannel)message.Channel).Guild.GetUserAsync(newestRecords.Id).Result.Username}");

                        Program.CurrentLevel = 0;
                        Program.CorrectNumber = 0;
                        currentGuesses = 0;
                        Program.IsNumberGameRunning = false;
                        Program.PlayingUser = null;
                    }
                }
            }

            if (message.Content.ToLower().Contains("hello") && message.Content.ToLower().Contains("csharpdewott"))
            {
                await message.Channel.SendMessageAsync($"Hello {message.Author.Username}!");
            }

            if (message.Content.Contains("(╯°□°）╯︵ ┻━┻"))
            {
                await message.Channel.SendMessageAsync("┬─┬﻿ ノ( ゜-゜ノ)");
            }

            if (message.Content.Contains("┬─┬﻿ ノ( ゜-゜ノ)"))
            {
                await message.Channel.SendMessageAsync("https://youtu.be/To6nhootM3w");
            }

            if (message.Content.ToLower().Contains("no u") && Globals.Random.NextDouble() < 0.3)
            {
                await message.Channel.SendMessageAsync("no u");
            }
        }
    }
}
