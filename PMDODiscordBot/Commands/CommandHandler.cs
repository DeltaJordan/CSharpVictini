namespace CSharpDewott.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Modules;
    using ESixOptions;
    using Extensions;
    using Logging;
    using Preconditions;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using Newtonsoft.Json;

    using Image = System.Drawing.Image;

    /// <summary>
    /// The command handler.
    /// </summary>
    public static class CommandHandler
    {
        /// <summary>
        /// The command service.
        /// </summary>
        private static CommandService commands;

        /// <summary>
        /// Initializes the command handler.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task InitializeCommandHandler()
        {
            commands = new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async
            });

            commands.Log += LogHandler.Log;

            Globals.CommandService = commands;
            
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        /// <summary>
        /// The pre-command handler.
        /// </summary>
        /// <param name="msg">
        /// The current message that triggered the handler.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task PreCommand(SocketMessage msg)
        {
            SocketUserMessage message = msg as SocketUserMessage;

            if (message == null || msg.Author.IsBot)
            {
                return;
            }

            if (Globals.Random.Next(65536) < 16)
            {
                await message.AddReactionAsync(Emote.Parse("351627395159031809"));
            }

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

            await HandleCommand(message);
        }

        /// <summary>
        /// Handles commands.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private static async Task HandleCommand(SocketUserMessage message)
        {
            int argPos = 0;

            if (message.HasCharPrefix('.', ref argPos))
            {
                // Create a Command Context
                CommandContext context = new CommandContext(Program.Client, message);

                string commandName = message.Content.Substring(1).Split(' ')[0].Trim();

                if (!AdminPrecondition.Whitelist.Contains(message.Author.Id) && !message.Author.IsBot && Globals.CommandService.Commands.Any(e => string.Equals(e.Name, message.Content.Substring(1).Split(' ')[0], StringComparison.CurrentCultureIgnoreCase) || e.Aliases.Any(f => string.Equals(f, message.Content.Substring(1).Split(' ')[0], StringComparison.CurrentCultureIgnoreCase))))
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
                // rather an object stating if the command executed successfully)
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

        /// <summary>
        /// Handles the message if it is not a command.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private static async Task PostCommand(SocketUserMessage message)
        {
            int luckyNumber = Globals.Random.Next(0, 1000);

            if (luckyNumber == 166)
            {
                await message.Channel.SendMessageAsync("This feels gay");
            }

            if (MiscCommands.NumberGameInstances.TryGetValue(message.Channel.Id, out NumberGame game) && message.Author.Id == game.PlayerUser.Id)
            {
                if (int.TryParse(message.Content, out int guess))
                {
                    await game.GuessNumber(guess);

                    if (game.IsGameOver)
                    {
                        MiscCommands.NumberGameInstances.Remove(message.Channel.Id);
                    }

                    return;
                }
            }

            if (message.Content.ToLower().Contains("hello") && message.MentionedUsers.Any(e => e.Id == Program.Client.CurrentUser.Id))
            {
                await message.Channel.SendMessageAsync($"Hello {message.Author.Username}!");
            }

            if (message.Content.Contains("(╯°□°）╯︵ ┻━┻"))
            {
                await message.Channel.SendMessageAsync("┬─┬﻿ ノ( ゜-゜ノ)");
                return;
            }

            if (message.Content.Contains("┬─┬﻿ ノ( ゜-゜ノ)"))
            {
                await message.Channel.SendMessageAsync("https://youtu.be/To6nhootM3w");
                return;
            }

            if (message.Content.ToLower().Contains("no u") && Globals.Random.NextDouble() < 0.3)
            {
                await message.Channel.SendMessageAsync("no u");
                return;
            }

            if (message.Content.ToLower().Contains("boo") && message.Content.ToLower().Contains("u") && message.MentionedUsers.Any(e => e.Id == Program.Client.CurrentUser.Id))
            {
                string booU = "boo";

                for (int i = 0; i < Globals.Random.Next(7); i++)
                {
                    booU += "o";
                }

                booU += " ";

                for (int i = -1; i < Globals.Random.Next(7); i++)
                {
                    booU += "u";
                }

                await message.Channel.SendMessageAsync($"no {booU} {message.Author.Mention}");
                return;
            }

            if (message.Content.ToLower().Contains("fuck") && (message.Content.ToLower().Contains("you") || message.Content.ToLower().Contains(" u")) && message.MentionedUsers.Any(e => e.Id == Program.Client.CurrentUser.Id))
            {
                await message.Channel.SendMessageAsync("~~Please do~~");
            }
        }
    }
}
