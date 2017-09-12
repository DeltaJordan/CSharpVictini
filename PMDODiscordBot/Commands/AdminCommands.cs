using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpDewott.Extensions;
using CSharpDewott.Preconditions;
using Discord;
using Discord.Commands;

namespace CSharpDewott.Commands
{
    public class AdminCommands : ModuleBase
    {
        [Command("stoptyping"), AdminPrecondition]
        public async Task StopTyping()
        {
            try
            {
                // Globals.TypingDisposable.ForEach(e => e.Dispose());
                // Globals.TypingDisposable.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("users"), AdminPrecondition]
        public async Task Users()
        {
            IGuild dewottGuild = await Context.Client.GetGuildAsync(329174505371074560);

            List<string> userList = (from guildUser in await dewottGuild.GetUsersAsync() select guildUser.Username).ToList();

            await this.ReplyAsync(string.Join(", ", userList));
        }

        [Command("eval"), AdminPrecondition]
        public async Task Eval(string inputCommand)
        {
            object obj = this.Context;

            foreach (string part in inputCommand.Split('.'))
            {
                string replacePart = part.Replace("▲", ".");

                if (obj == null)
                {
                    await this.ReplyAsync($"obj returned null on command part before \"{replacePart}\"!");
                    return;
                }

                Type type = obj.GetType();
                PropertyInfo propertyInfo = type.GetProperties().FirstOrDefault(e => string.Equals(e.Name, replacePart, StringComparison.CurrentCultureIgnoreCase));
                if (propertyInfo == null)
                {
                    MethodInfo methodInfo = type.GetMethods().FirstOrDefault(e => string.Equals(e.Name, Regex.Match(replacePart, @"[^\(]+").Value, StringComparison.CurrentCultureIgnoreCase));

                    if (methodInfo == null)
                    {
                        await this.ReplyAsync($"info returned null on command part \"{replacePart}\" (Regex:{Regex.Match(replacePart, @"[^(]+").Value})!");
                        return;
                    }

                    List<object> parameterList = new List<object>();

                    foreach (string s in replacePart.Replace(Regex.Match(replacePart, @"[^\(]+").Value, string.Empty).Replace(")", string.Empty).Replace("(", string.Empty).Split(',').Select(e => e.Trim()))
                    {
                        if (ulong.TryParse(s, out ulong result))
                        {
                            parameterList.Add(result);
                        }
                        else if (bool.TryParse(s, out bool boolResult))
                        {
                            parameterList.Add(boolResult);
                        }
                        else if (s.ToLower() == "null")
                        {
                            parameterList.Add(null);
                        }
                        else
                        {
                            parameterList.Add(s);
                        }
                    }

                    obj = methodInfo.Invoke(obj, parameterList.ToArray());
                }
                else
                {
                    obj = propertyInfo.GetValue(obj, null);
                }
            }

            await this.ReplyAsync(obj.ToString());
        }

        [Command("rebuildlogs"), AdminPrecondition]
        public async Task RebuildLogs()
        {
            try
            {
                await Program.Client_Ready();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("perms"), AdminPrecondition]
        public async Task Perms(IGuildUser user)
        {
            if (user != null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "Permissions",
                    Value = user.GuildPermissions.ToList().Aggregate(string.Empty, (current, userGuildPermission) => current + ", " + Enum.GetName(typeof(GuildPermission), userGuildPermission))
                });

                builder.Author = new EmbedAuthorBuilder
                {
                    IconUrl = user.GetAvatarUrl(),
                    Name = user.GetUsernameOrNickname()
                };

                await this.ReplyAsync(string.Empty, false, builder.Build());
            }
        }

        [Command("add_blacklist"), Summary("This is a bot owner only command, which means you can't use it ¯\\_(ツ)_/¯"), AdminPrecondition]
        public async Task BlacklistAdd(string commandName)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Program.AppPath, "blacklists"));

                List<string> blacklists = new List<string> { this.Context.Channel.Id.ToString() };

                if (File.Exists(Path.Combine(Program.AppPath, "blacklists", $"{commandName}.txt")))
                {
                    blacklists.AddRange(File.ReadAllLines(Path.Combine(Program.AppPath, "blacklists", $"{commandName}.txt")));
                }

                blacklists = blacklists.Distinct().ToList();

                File.WriteAllLines(Path.Combine(Program.AppPath, "blacklists", $"{commandName}.txt"), blacklists);

                await this.ReplyAsync($"{commandName} has been blacklisted from channel {this.Context.Channel.Name}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("add_blacklist_markov"), Summary("This is a bot owner only command, which means you can't use it ¯\\_(ツ)_/¯"), AdminPrecondition]
        public async Task BlacklistMarkovAdd()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Program.AppPath, "markov-blacklists"));

                List<string> blacklists = new List<string> { this.Context.Channel.Name };

                if (File.Exists(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt")))
                {
                    blacklists.AddRange(File.ReadAllLines(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt")));
                }

                blacklists = blacklists.Distinct().ToList();

                File.WriteAllLines(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt"), blacklists);

                await this.ReplyAsync($"Markovs will no longer pull text from {this.Context.Channel.Name}.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("purge"), Summary("This is a bot owner only command, which means you can't use it ¯\\_(ツ)_/¯"), AdminPrecondition]
        public async Task Purge(int numberOfMessages)
        {
            if (numberOfMessages == -1)
            {
                List<IReadOnlyCollection<IMessage>> messagesList = await this.Context.Channel.GetMessagesAsync(int.MaxValue).ToList();

                List<IMessage> messages = new List<IMessage>();

                foreach (IReadOnlyCollection<IMessage> messages1 in messagesList)
                {
                    IReadOnlyCollection<IMessage> readOnlyCollection = messages1;
                    messages.AddRange(readOnlyCollection);
                }

                foreach (IMessage userMessage in messages)
                {
                    await userMessage.DeleteAsync();
                }
            }
            else
            {
                List<IReadOnlyCollection<IMessage>> messagesList = await this.Context.Channel.GetMessagesAsync(int.MaxValue).ToList();

                List<IMessage> messages = new List<IMessage>();

                foreach (IReadOnlyCollection<IMessage> messages1 in messagesList)
                {
                    IReadOnlyCollection<IMessage> readOnlyCollection = messages1;
                    messages.AddRange(readOnlyCollection);
                }

                messages.Sort((e, f) => e.Timestamp.CompareTo(f.Timestamp));

                for (int index = 0; index < numberOfMessages + 1; index++)
                {
                    await messages[index].DeleteAsync();
                }
            }
        }

        [Command("setgame"), AdminPrecondition, Summary("Sets the \"Playing\" text")]
        public async Task SetGameText(string gameText)
        {
            await Program.Client.SetGameAsync(gameText);
        }
    }
}
