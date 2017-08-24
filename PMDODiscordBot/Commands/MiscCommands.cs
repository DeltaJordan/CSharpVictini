// <copyright file="Commands.cs" company="JordantheBuizel">
// Copyright (c) JordantheBuizel. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Deserialization;
using CSharpDewott.ESixOptions;
using CSharpDewott.Extensions;
using CSharpDewott.GameInfo;
using CSharpDewott.IO;
using CSharpDewott.Music;
using CSharpDewott.Preconditions;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using IMessage = Discord.IMessage;
using ParameterInfo = Discord.Commands.ParameterInfo;

namespace CSharpDewott.Commands
{
    public class MiscCommands : ModuleBase
    {
        private IAudioClient currentAudioClient;
        private Process ffmpeg;
        private readonly string AppPath = Program.AppPath;

        //Cooldowns
        private int lastUsermarkovCall = 0;
        private int lastSongSwitch;

        private Dictionary<int, string> staleList = new Dictionary<int, string>();

        private readonly List<string> blackListSongs = new List<string>
        {
            "tank engine",
            "pmu"
        };

        private Process CreateStream(string songPath)
        {
            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{songPath}\" -ac 2 -f s16le -ar 48000 pipe:1",
                Verb = "runas",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpegStartInfo);
        }

        private async void AutoPlayNextSong()
        {
            string path = string.Empty;

            try
            {
                WebClient client = new WebClient();
                using (MemoryStream musicTrackListXml = new MemoryStream(client.DownloadData("ftp://localhost/Tracks/TrackList.xml")))
                {
                    TrackList trackList = new TrackList();
                    musicTrackListXml.Seek(0, SeekOrigin.Begin);
                    using (XmlReader reader = XmlReader.Create(musicTrackListXml))
                    {
                        trackList.Load(reader);
                    }

                    string selectedTrackName = trackList.Entries[Globals.Random.Next(0, trackList.Entries.Count - 1)].TrackName;

                    if (this.blackListSongs.Any(blackListSong => selectedTrackName.ToLower().Contains(blackListSong)))
                    {
                        this.AutoPlayNextSong();
                        return;
                    }

                    if (!File.Exists(Path.Combine(Program.AppPath, "Tracks", selectedTrackName)))
                    {
                        WebClient songClient = new WebClient();
                        using (MemoryStream musicDownload = new MemoryStream(songClient.DownloadData($"ftp://localhost/Tracks/{selectedTrackName}")))
                        {
                            File.WriteAllBytes(Path.Combine(PathsHelper.CreateIfDoesNotExist(Program.AppPath, "Tracks"), selectedTrackName), musicDownload.ToArray());

                            path = Path.Combine(Program.AppPath, "Tracks", selectedTrackName);
                        }
                    }

                    string trackInfoBuilder = string.Empty;

                    selectedTrackName = selectedTrackName.Replace(".ogg", string.Empty);

                    int firstParenthase = selectedTrackName.Contains(")") ? selectedTrackName.IndexOf(")") : 0;

                    if (firstParenthase > 0)
                    {

                        trackInfoBuilder += "```\nTrack:\n";
                        trackInfoBuilder += $"{selectedTrackName.Substring(firstParenthase + 1)}\n\n";
                        trackInfoBuilder += "Origin (probably abbreviated):\n";
                        trackInfoBuilder += $" {selectedTrackName.Split(')')[0]}\n\n";
                        trackInfoBuilder += "```";
                    }
                    else
                    {
                        trackInfoBuilder += "```\nTrack:\n";
                        trackInfoBuilder += $"{selectedTrackName}\n```";
                    }

                    await (await this.Context.Guild.GetTextChannelAsync(335897183754911744)).SendMessageAsync(trackInfoBuilder);
                }

                if (this.ffmpeg != null && !this.ffmpeg.HasExited)
                {
                    this.ffmpeg.Close();
                }

                this.ffmpeg = this.CreateStream(path);
                Stream output = this.ffmpeg.StandardOutput.BaseStream;
                AudioOutStream discord = this.currentAudioClient.CreatePCMStream(AudioApplication.Mixed, 98304);
                await output.CopyToAsync(discord);
                await discord.FlushAsync();

                this.AutoPlayNextSong();

            }
            catch (WebException e)
            {
                Console.Out.WriteLine(e);

                await this.Context.Channel.SendMessageAsync("Our music server is offline, sorry about that! Playing from local storage instead...");

                this.AutoPlayNextSongLocal();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }

        private async void AutoPlayNextSongLocal()
        {
            string path = string.Empty;

            try
            {
                List<int> validInts = new List<int>();

                string[] musicFiles = Directory.GetFiles(Path.Combine(Program.AppPath, "Tracks"));

                for (int i = 0; i < musicFiles.Length; i++)
                {
                    if (this.staleList.ContainsKey(i))
                    {
                        continue;
                    }

                    validInts.Add(i);
                }

                if (musicFiles.Length == this.staleList.Count)
                {
                    this.staleList.Clear();
                }

                string selectedTrackName = musicFiles[validInts[Globals.Random.Next(0, musicFiles.Length - 1)]];

                if (this.blackListSongs.Any(blackListSong => selectedTrackName.ToLower().Contains(blackListSong)))
                {
                    this.AutoPlayNextSong();
                    return;
                }

                path = Path.Combine(Program.AppPath, "Tracks", selectedTrackName);

                string trackInfoBuilder = string.Empty;

                selectedTrackName = selectedTrackName.Replace(".ogg", string.Empty);

                int firstParenthase = selectedTrackName.Contains(")") ? selectedTrackName.IndexOf(")") : 0;

                if (firstParenthase > 0)
                {

                    trackInfoBuilder += "```\nTrack:\n";
                    trackInfoBuilder += $"{selectedTrackName.Substring(firstParenthase + 1)}\n\n";
                    trackInfoBuilder += "Origin (probably abbreviated):\n";
                    trackInfoBuilder += $" {selectedTrackName.Split(')')[0]}\n\n";
                    trackInfoBuilder += "```";
                }
                else
                {
                    trackInfoBuilder += "```\nTrack:\n";
                    trackInfoBuilder += $"{selectedTrackName}\n```";
                }

                await (await this.Context.Guild.GetTextChannelAsync(335897183754911744)).SendMessageAsync(trackInfoBuilder);

                if (this.ffmpeg != null && !this.ffmpeg.HasExited)
                {
                    this.ffmpeg.Close();
                }

                this.ffmpeg = this.CreateStream(path);
                Stream output = this.ffmpeg.StandardOutput.BaseStream;
                AudioOutStream discord = this.currentAudioClient.CreatePCMStream(AudioApplication.Mixed, 98304);
                await output.CopyToAsync(discord);
                await discord.FlushAsync();

                this.AutoPlayNextSongLocal();

            }
            catch (WebException e)
            {
                Console.Out.WriteLine(e);

                await this.Context.Channel.SendMessageAsync("Our music server is offline, sorry about that! Playing from local storage instead...");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }

        [Command("poll")]
        [Summary("Creates a poll with an option to vote up or down")]
        public async Task Poll(
            int time,
            [Summary("Title to display for the poll.")]
            params string[] titleStrings)
        {
            string title = string.Join(" ", titleStrings);

            IUserMessage message = await this.ReplyAsync($"Vote on {this.Context.User.Username}'s poll with reactions\n**{title}**\nYou have {time} seconds to vote.");
            
            await message.AddReactionAsync(new Emoji("👍"));

            await message.AddReactionAsync(new Emoji("👎"));

            await Task.Delay(new TimeSpan(0, 0, 0, time));
            
            EmbedBuilder resultsbuilder = new EmbedBuilder();

            IUserMessage updatedmessage = (IUserMessage)await this.Context.Channel.GetMessageAsync(message.Id);

            int upReactionCount = updatedmessage.Reactions.Values.Where(e => e.IsMe).ElementAt(0).ReactionCount - 1;

            int downReactionCount = updatedmessage.Reactions.Values.Where(e => e.IsMe).ElementAt(1).ReactionCount - 1;

            resultsbuilder.Description = $"👍 {upReactionCount}             👎 {downReactionCount}";

            await this.ReplyAsync($"Results of {this.Context.User.Mention}'s poll \"{title}\":", false, resultsbuilder.Build());

            foreach (KeyValuePair<IEmote, ReactionMetadata> updatedmessageReaction in updatedmessage.Reactions)
            {
                Console.Out.WriteLine($"Emote {updatedmessageReaction.Key.Name} with {updatedmessageReaction.Value.ReactionCount} reactions");
            }
        }

        [Command("2017"), Summary("2017 memes")]
        public async Task Task2017(params string[] input)
        {
            await this.ReplyAsync($">2017\n>{string.Join(" ", input)}");
        }

        [Command("scream")]
        public async Task Scream()
        {
            string scream = "REEEEEEEE";
            for (int i = 0; i < Globals.Random.Next(1, 50); i++)
            {
                scream += "E";
            }

            await this.ReplyAsync($"__***{scream}***__");
        }

        [Command("roll")]
        public async Task Roll(float max)
        {
            if (max < 0)
            {
                max = Math.Abs(max);
            }

            int maxRandVal = max > int.MaxValue ? int.MaxValue : (int)max;

            await this.ReplyAsync($"Rolled {Globals.Random.Next(1, maxRandVal > 0 ? maxRandVal : 1)}");
        }
        
        [Command("connect")]
        [Summary("Randomly plays a different song. Can only be called by non-admin once every minute\nIf the bot disconnects from the voice channel this will also reconnect it."), AdminPrecondition]
        public async Task JoinVoiceChannel(IVoiceChannel channel = null)
        {

            channel = channel ?? (await this.Context.Guild.GetVoiceChannelsAsync()).First(e => e.Name.ToLower().Contains("music"));

            this.currentAudioClient = await channel.ConnectAsync();

            this.lastSongSwitch = Environment.TickCount;

            this.AutoPlayNextSong();
        }

        /*[Command("testskip2")]
        public async Task NextSong()
        {
            if (!this.MeAndKikiWhoops.Contains(this.Context.User.Id))
            {
                return;
            }

            string path = string.Empty;

            try
            {
                WebClient client = new WebClient();
                using (MemoryStream musicTrackListXml = new MemoryStream(client.DownloadData("ftp://localhost/Tracks/TrackList.xml")))
                {
                    Music.TrackList trackList = new Music.TrackList();
                    musicTrackListXml.Seek(0, SeekOrigin.Begin);
                    using (XmlReader reader = XmlReader.Create(musicTrackListXml))
                    {
                        trackList.Load(reader);
                    }

                    string selectedTrackName = trackList.Entries[Globals.Random.Next(0, trackList.Entries.Count - 1)].TrackName;

                    if (this.blackListSongs.Any(blackListSong => selectedTrackName.ToLower().Contains(blackListSong)))
                    {
                        this.AutoPlayNextSong();
                        return;
                    }


                    if (!File.Exists(Path.Combine(Program.AppPath, "Tracks", selectedTrackName)))
                    {
                        WebClient songClient = new WebClient();
                        using (MemoryStream musicDownload = new MemoryStream(songClient.DownloadData($"ftp://localhost/Tracks/{selectedTrackName}")))
                        {
                            File.WriteAllBytes(Path.Combine(PathsHelper.CreateIfDoesNotExist(Program.AppPath, "Tracks"), selectedTrackName), musicDownload.ToArray());

                            path = Path.Combine(Program.AppPath, "Tracks", selectedTrackName);
                        }
                    }

                    string trackInfoBuilder = string.Empty;

                    selectedTrackName = selectedTrackName.Replace(".ogg", string.Empty);

                    int firstParenthase = selectedTrackName.Contains(")") ? selectedTrackName.IndexOf(")") : 0;

                    if (firstParenthase > 0)
                    {

                        trackInfoBuilder += "```\nTrack:\n";
                        trackInfoBuilder += $"{selectedTrackName.Substring(firstParenthase + 1)}\n\n";
                        trackInfoBuilder += "Origin (probably abbreviated):\n";
                        trackInfoBuilder += $" {selectedTrackName.Split(')')[0]}\n\n";
                        trackInfoBuilder += "```";
                    }
                    else
                    {
                        trackInfoBuilder += "```\nTrack:\n";
                        trackInfoBuilder += $"{selectedTrackName}\n```";
                    }

                    await (await this.Context.Guild.GetTextChannelAsync(323211862097526784)).SendMessageAsync(trackInfoBuilder);
                }

                if (this.ffmpeg != null && !this.ffmpeg.HasExited)
                {
                    this.ffmpeg.Close();
                }

                this.ffmpeg = this.CreateStream(path);
                Stream output = this.ffmpeg.StandardOutput.BaseStream;
                AudioOutStream discord = this.currentAudioClient.CreatePCMStream(AudioApplication.Mixed, 98304);
                await output.CopyToAsync(discord);
                await discord.FlushAsync();

                this.AutoPlayNextSong();

            }
            catch (WebException e)
            {
                Console.Out.WriteLine(e);

                await this.Context.Channel.SendMessageAsync("Our music server is offline, sorry about that! Playing from local storage instead...");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }*/

        [Command("numbergame")]
        [Summary("Starts a number game, where you guess the generated number based on the hint.")]
        public async Task NumberGame(
            [Summary("Optionally choose the level. 1 = Easy, 2 = Medium, 3 = Hard, 4 = Extreme")]
            int level = 1)
        {
            if (Program.IsNumberGameRunning)
            {
                await this.Context.Channel.SendMessageAsync("Please wait until the current game is finished.");
                return;
            }

            if (!this.Context.Channel.Name.Contains("bot"))
            {
                await this.ReplyAsync("Please use the bot channel for number game");
                return;
            }

            Program.CurrentLevel = level;

            try
            {

                switch (level)
                {
                    case 1:
                    {
                        await this.Context.Channel.SendMessageAsync("Starting number game with easy difficulty...");
                        IDisposable typeDisposable = this.Context.Channel.EnterTypingState();
                        Program.CorrectNumber = Program.Random.Next(1, 100);
                        Program.IsNumberGameRunning = true;
                        Program.PlayingUser = this.Context.User;
                        typeDisposable.Dispose();
                        await this.Context.Channel.SendMessageAsync(
                            "Begin guessing! The number is between 1 and 100");
                    }

                        break;
                    case 2:
                    {
                        await this.Context.Channel.SendMessageAsync("Starting number game with medium difficulty...");
                        IDisposable typeDisposable = this.Context.Channel.EnterTypingState();
                        Program.CorrectNumber = Program.Random.Next(1, 1000);
                        Program.IsNumberGameRunning = true;
                        Program.PlayingUser = this.Context.User;
                        typeDisposable.Dispose();
                            await this.Context.Channel.SendMessageAsync(
                            "Begin guessing! The number is between 1 and 1000");
                    }

                        break;
                    case 3:
                    {
                        await this.Context.Channel.SendMessageAsync("Starting number game with hard difficulty...");
                        IDisposable typeDisposable = this.Context.Channel.EnterTypingState();
                        Program.CorrectNumber = Program.Random.Next(1, 10000);
                        Program.IsNumberGameRunning = true;
                        Program.PlayingUser = this.Context.User;
                        typeDisposable.Dispose();
                            await this.Context.Channel.SendMessageAsync(
                            "Begin guessing! The number is between 1 and 10000");
                    }

                        break;
                    case 4:
                    {
                        await this.Context.Channel.SendMessageAsync("Starting number game with extreme difficulty...");
                        IDisposable typeDisposable = this.Context.Channel.EnterTypingState();
                        Program.CorrectNumber = Program.Random.Next(1, 100000);
                        Program.IsNumberGameRunning = true;
                        Program.PlayingUser = this.Context.User;
                        typeDisposable.Dispose();
                            await this.Context.Channel.SendMessageAsync(
                            "Begin guessing! The number is between 1 and 100000");
                    }

                        break;
                    default:
                    {
                        Program.CurrentLevel = 1;
                        await this.Context.Channel.SendMessageAsync("Starting number game with easy difficulty...");
                        IDisposable typeDisposable = this.Context.Channel.EnterTypingState();
                        Program.CorrectNumber = Program.Random.Next(1, 100);
                        Program.IsNumberGameRunning = true;
                        Program.PlayingUser = this.Context.User;
                        typeDisposable.Dispose();
                            await this.Context.Channel.SendMessageAsync(
                            "Begin guessing! The number is between 1 and 100");
                    }

                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine(ex);
            }
        }

        /*[Command("me")]
        [Summary("Better /me command, very similar to the PMDO one.")]
        public async Task Say([Summary("This parameter must be in quotes.")]string text)
        {
            await this.Context.Channel.SendMessageAsync($"{this.Context.User.Username} is {text}");
        }

        [Command("definition")]
        [Summary("Gets the definition of a word, along with some other info")]
        public async Task Definition(string word)
        {
            File.WriteAllText
                (
                Path.Combine(Program.AppPath, "wordapirequests.txt"), 
                !File.Exists(Path.Combine(Program.AppPath, "wordapirequests.txt")) ? "1" : (int.Parse(File.ReadAllText(Path.Combine(Program.AppPath, "wordapirequests.txt"))) + 1).ToString()
                );

            if (int.Parse() >= 2000)
            {
                
            }

            HttpResponse<string> response = Unirest.get("https://wordsapiv1.p.mashape.com/words/bump/also")
                .header("X-Mashape-Key", "UD7da7oyXMmshZUsIccgFUHjW2Acp1LhuwEjsnPiViVKD13g6B")
                .header("Accept", "application/json")
                .asJson<string>();
        }*/

        [Command("getfrisky")]
        public async Task SetAdeventurer()
        {
            IGuild mainGuild = await this.Context.Client.GetGuildAsync(329174505371074560);

            if (mainGuild != null)
            {
                try
                {
                    if (await mainGuild.GetUserAsync(this.Context.User.Id) == null)
                    {
                        await this.Context.Channel.SendMessageAsync("This bot has been moved to a private server. Sorry...");
                        return;
                    }

                    if ((await mainGuild.GetUserAsync(this.Context.User.Id)).RoleIds.Any(e => e == 344712612560502796))
                    {
                        await this.Context.Channel.SendMessageAsync(
                            "You have already been given the \"Adventurous\" role!");
                        return;
                    }

                    await (await mainGuild.GetUserAsync(this.Context.User.Id)).AddRoleAsync(mainGuild.GetRole(344712612560502796));
                    await this.Context.Channel.SendMessageAsync(
                        "You have been given the \"Adventurous\" role! Hmm wonder what it does 🤔");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteLine(ex);
                }
            }
        }

        [Command("getunfrisky")]
        public async Task RemoveAdeventurer()
        {
            IGuild mainGuild = await this.Context.Client.GetGuildAsync(329174505371074560);

            if (mainGuild != null)
            {
                try
                {
                    if (await mainGuild.GetUserAsync(this.Context.User.Id) == null)
                    {
                        await this.Context.Channel.SendMessageAsync("This bot has been moved to a private server. Sorry...");
                        return;
                    }

                    if ((await mainGuild.GetUserAsync(this.Context.User.Id)).RoleIds.All(e => e != 344712612560502796))
                    {
                        await this.Context.Channel.SendMessageAsync("You do not have the \"Adventurous\" role!");
                        return;
                    }

                    await (await mainGuild.GetUserAsync(this.Context.User.Id)).RemoveRoleAsync(mainGuild.GetRole(344712612560502796));
                    await this.Context.Channel.SendMessageAsync("You no longer have the \"Adventurous\" role.");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteLine(ex);
                }
            }
        }

        /*[Summary("Suggest something to the #suggestions channel!")]
        [Command("suggest")]
        public async Task Suggest(
            [Summary("Suggestion text to send to #suggestions. Must be in quotes")]
            string text)
        {
            IGuild pmdoPublic = await this.Context.Client.GetGuildAsync(280822504824766464);

            if (pmdoPublic != null)
            {
                try
                {
                    if (pmdoPublic.GetUserAsync(this.Context.User.Id) == null)
                    {
                        await this.Context.Channel.SendMessageAsync("You are not in the PMDO public server!");
                        return;
                    }

                    await ((await pmdoPublic.GetChannelAsync(280822796978749441)) as ITextChannel).SendMessageAsync($"Suggestion by {this.Context.User.Username}: {text}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteLine(ex.Message);
                    await this.Context.Channel.SendMessageAsync("You are not in the PMDO public server!");
                }
            }
        }*/

        [Command("help")]
        public async Task Help(string text = "")
        {

            Image image = null;

            if (HttpHelper.UrlExists("https://play.pokemonshowdown.com/sprites/xyani/dewott.gif"))
            {
                byte[] imageBytes = await Program.Instance.HttpClient.GetByteArrayAsync("https://play.pokemonshowdown.com/sprites/xyani/dewott.gif");

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    image = Image.FromStream(ms);
                }
            }

            if (image != null)
            {
                if (File.Exists(Path.Combine(this.AppPath, "pokemon.gif")))
                {
                    File.Delete(Path.Combine(this.AppPath, "pokemon.gif"));
                }

                image.Save(Path.Combine(this.AppPath, "pokemon.gif"));

                await (await this.Context.User.GetOrCreateDMChannelAsync()).SendFileAsync(Path.Combine(this.AppPath, "pokemon.gif"));
            }

            if (text == string.Empty)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(Color.FromArgb(163, 214, 227));
                builder.Title = "List of commands:";

                string commands = string.Empty;

                foreach (CommandInfo commandServiceCommand in Globals.CommandService.Commands)
                {
                    if (!(await commandServiceCommand.CheckPreconditionsAsync(this.Context)).IsSuccess)
                    {
                        continue;
                    }

                    commands += $"{commandServiceCommand.Name}\n";
                }

                builder.Description = commands;

                await (await this.Context.User.GetOrCreateDMChannelAsync()).SendMessageAsync(string.Empty, false, builder.Build());
            }
            else if (Globals.CommandService.Commands.ToList().Find(e => e.Name == text) != null)
            {
                EmbedBuilder builder = new EmbedBuilder();

                CommandInfo requestedInfo = Globals.CommandService.Commands.ToList().Find(e => e.Name == text);

                if (!(await requestedInfo.CheckPreconditionsAsync(this.Context)).IsSuccess)
                {
                    await this.ReplyAsync((await requestedInfo.CheckPreconditionsAsync(this.Context)).ErrorReason);
                    return;
                }

                builder.Title = text;
                builder.Description = string.Empty;

                if (requestedInfo.Parameters.Count > 0)
                {
                    foreach (ParameterInfo parameter in requestedInfo.Parameters)
                    {
                        builder.Title += $" [{parameter.Name}{(parameter.IsOptional ? $" = {Convert.ToString(parameter.DefaultValue)}" : string.Empty)}] ";
                    }
                }

                if (requestedInfo.Aliases.Count > 0)
                {
                    builder.Description += "\nAliases: ";
                    builder.Description = requestedInfo.Aliases.Aggregate(builder.Description, (current, alias) => current + $"{alias} ");
                }

                if (!string.IsNullOrWhiteSpace(requestedInfo.Summary))
                {
                    builder.Description += $"\n{requestedInfo.Summary}\n";
                }

                if (requestedInfo.Parameters.Count > 0)
                {
                    builder.Description = requestedInfo.Parameters.Where(parameter => !string.IsNullOrWhiteSpace(parameter.Summary)).Aggregate(builder.Description, (current, parameter) => current + $"\n{parameter.Name}: {parameter.Summary}");
                }

                await this.ReplyAsync(string.Empty, false, builder.Build());
            }
            else
            {
                Dictionary<string, string> allAliasDict = new Dictionary<string, string>();

                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = string.Empty,
                    Description = string.Empty
                };

                foreach (CommandInfo cmdInfo in Globals.CommandService.Commands)
                {
                    foreach (string cmdInfoAlias in cmdInfo.Aliases)
                    {
                        allAliasDict.Add(cmdInfoAlias, cmdInfo.Name);
                    }
                }

                if (allAliasDict.TryGetValue(text, out string result))
                {
                    CommandInfo requestedInfo = Globals.CommandService.Commands.ToList().Find(e => e.Name == result);

                    builder.Title = result;

                    if (requestedInfo.Parameters.Count > 0)
                    {
                        foreach (ParameterInfo parameter in requestedInfo.Parameters)
                        {
                            builder.Title += $" [{parameter.Name}{(parameter.IsOptional ? $" = {Convert.ToString(parameter.DefaultValue)}" : string.Empty)}] ";
                        }
                    }

                    if (requestedInfo.Aliases.Count > 0)
                    {
                        builder.Description += "Aliases: ";
                        builder.Description = requestedInfo.Aliases.Aggregate(builder.Description, (current, alias) => current + $"{alias} ");
                    }

                    if (!string.IsNullOrWhiteSpace(requestedInfo.Summary))
                    {
                        builder.Description += $"\n{requestedInfo.Summary}\n";
                    }

                    if (requestedInfo.Parameters.Count > 0)
                    {
                        foreach (ParameterInfo parameter in requestedInfo.Parameters)
                        {
                            if (!string.IsNullOrWhiteSpace(parameter.Summary))
                            {
                                builder.Description += $"\n{parameter.Name}: {parameter.Summary}";
                            }
                        }
                    }

                    await this.ReplyAsync(string.Empty, false, builder.Build());
                }
                else
                {
                    await this.ReplyAsync("Command not found!");
                }
            }
        }

        [Command("botinfo"), Summary("Gets info about CSharpDewott#5238.")]
        public async Task BotInfo()
        {
            IGuildUser botGuildUser = await this.Context.Guild.GetUserAsync(this.Context.Client.CurrentUser.Id);

            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "CSharpDewott#5238, created by Jordan Zeotni (JordantheDewott#8352)",
                ThumbnailUrl = "https://play.pokemonshowdown.com/sprites/xyani/dewott.gif",
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Created on:",
                        Value = this.Context.Client.CurrentUser.CreatedAt
                    },

                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Id",
                        Value = this.Context.Client.CurrentUser.Id
                    },

                    new EmbedFieldBuilder
                    {
                        IsInline = false,
                        Name = "Permissions",
                        Value = string.Join(", ", botGuildUser.GuildPermissions.ToList().Select(e => Enum.GetName(typeof(GuildPermission), e)))
                    }
                }
            };

            await this.ReplyAsync(string.Empty, false, builder.Build());

        }

        [Summary("Get info about the user, or another user if requested"), Command("userinfo"), Alias("user")]
        public async Task UserInfo(
            [Summary("Optional user to request. Must be in quotes if there is a space in the name.")]
            IGuildUser user = null)
        {
            if (this.Context.Channel is IDMChannel)
            {
                return;
            }

            if (user == null)
            {
                user = (IGuildUser)this.Context.User;
            }

            EmbedBuilder builder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = user.Username,
                    IconUrl = user.GetAvatarUrl()
                }
            };
            

            if (!string.IsNullOrWhiteSpace(user.Nickname))
            {
                builder.Fields.Add(new EmbedFieldBuilder
                {
                    Name = "Nickname:",
                    Value = user.Nickname
                });
            }

            builder.Fields.Add(new EmbedFieldBuilder
            {
                Name = "User ID:",
                Value = user.Id
            });
            builder.Fields.Add(new EmbedFieldBuilder
            {
                Name = "Created at:",
                Value = user.CreatedAt
            });
            builder.Fields.Add(new EmbedFieldBuilder
            {
                Name = "Joined at:",
                Value = user.JoinedAt
            });

            string roleBuilder = user.RoleIds.Where(userRoleId => !this.Context.Guild.GetRole(userRoleId).Name.Contains("everyone")).Aggregate(string.Empty, (current, userRoleId) => current + $"{this.Context.Guild.GetRole(userRoleId).Name}, ");

            roleBuilder = roleBuilder.TrimEnd(',', ' ');

            builder.Fields.Add(new EmbedFieldBuilder
            {
                Name = "Roles:",
                Value = roleBuilder
            });

            builder.ImageUrl = user.GetAvatarUrl();
            builder.WithColor(user.RoleIds.FirstOrDefault() != 0 ? this.Context.Guild.GetRole(user.RoleIds.FirstOrDefault()).Color : Discord.Color.LightGrey);

            await this.Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
        }

        [Command("color"), Summary("Gets info of a color based on either a hex or a color's name")]
        public async Task ColorTask(string input)
        {
            try
            {
                Color result = Color.Empty;

                if (input.StartsWith("#") && input.Length == 7)
                {
                    result = ColorTranslator.FromHtml(input);
                }
                else if (input.StartsWith("#") && input.Length == 9)
                {
                    int argb = int.Parse(input.Replace("#", ""), NumberStyles.HexNumber);
                    result = Color.FromArgb(argb);
                }
                else
                {
                    try
                    {
                        result = Color.FromName(input);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(result);
                builder.Title = result.Name;
                builder.Description = string.Empty;
                builder.Description += $"Hex:               #{result.R:X2}{result.G:X2}{result.B:X2}\n";
                builder.Description += $"R {result.R}:G {result.G}:B {result.B}";

                if ($"{result.R:X2}{result.G:X2}{result.B:X2}" != "000000")
                {
                    await this.ReplyAsync(string.Empty, false, builder.Build());
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine(ex);
            }
        }

        [Command("stats"), Summary("Gets random info about the server."), Alias("stat", "serverinfo")]
        public async Task Stats()
        {
            Dictionary<ulong, DeserializedMessage> allCachedMessages = Program.LogMessages;

            EmbedBuilder builder = new EmbedBuilder
            {
                Title = $"Stats for {this.Context.Guild.Name}",
                ThumbnailUrl = this.Context.Guild.IconUrl
            };

            List<int> messegesInAWeek = new List<int>();

            for (int i = 1; i < 7; i++)
            {
                if (DateTime.Now.Day - i > 0)
                {
                    messegesInAWeek.Add(allCachedMessages.Values.Count(e => e.Timestamp.Day == DateTime.Now.Day - i && e.Timestamp.Month == DateTime.Now.Month));
                }
                else
                {
                    int overflow = i - DateTime.Now.Day;

                    int month = DateTime.Now.Month - 1;

                    int day = DateTime.DaysInMonth(DateTime.Now.Year, month) - overflow;

                    messegesInAWeek.Add(allCachedMessages.Values.Count(e => e.Timestamp.Day == day && e.Timestamp.Month == month));
                }
            }

            int averageMessages = messegesInAWeek.Sum() / messegesInAWeek.Count;

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Average messeges per day in the last week",
                Value = averageMessages
            });

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Total messages in server's lifetime",
                Value = allCachedMessages.Count
            });

            Dictionary<ulong, int> userMessageCountDictionary = (await this.Context.Guild.GetUsersAsync()).ToDictionary(guildUser => guildUser.Id, guildUser => allCachedMessages.Values.Count(e => e.Author.Id == guildUser.Id));

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "User with the most messages",
                Value = (await this.Context.Guild.GetUserAsync(userMessageCountDictionary.Aggregate((l, r) => l.Value > r.Value ? l : r).Key)).Username
            });

            Dictionary<ulong, int> channelMessageCountDictionary = (await this.Context.Guild.GetTextChannelsAsync()).ToDictionary(channel => channel.Id, channel => allCachedMessages.Values.Count(e => e.Channel.Id == channel.Id));

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Channel with the most messages",
                Value = (await this.Context.Guild.GetTextChannelAsync(channelMessageCountDictionary.Aggregate((l, r) => l.Value > r.Value ? l : r).Key)).Name
            });

            /*string mostCommon5 = allCachedMessages
                .SelectMany(e => 
                    Regex.Matches(e.Content, @"[A-Za-z-']+")
                        .OfType<Match>()
                        .Select(match => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Value))
                        .GroupBy(word => word)
                        .Select(chunk => new
                        {
                            word = chunk.Key,
                            count = chunk.Count()
                        })
                        .OrderByDescending(item => item.count)
                        .ThenBy(item => item.word)
                        .Take(5).Select(f => f.word))
                .Aggregate((a, b) => a + ", " + b);

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Top 5 words",
                Value = mostCommon5
            });*/

            await this.ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("why")]
        public async Task Why()
        {
            await this.ReplyAsync("https://www.youtube.com/watch?v=XW5Lmq4mwQs");
        }

        private class MessageComparer : IComparer<IMessage>
        {
            public int Compare(IMessage x, IMessage y)
            {
                if (x != null && y != null)
                {
                    return x.Timestamp.CompareTo(y.Timestamp);
                }

                throw new NullReferenceException();
            }
        }
    }

    [Group("gameinfo")]
    public class GameInfo : ModuleBase
    {
        [Command("get"), Summary("Retrieves game info from requested user.")]
        public async Task GetGameInfo(IUser user = null, string game = null)
        {
            if (user == null)
            {
                user = this.Context.User;
            }

            List<Player> players = JsonConvert.DeserializeObject<List<Player>>(File.ReadAllText(Path.Combine(Program.AppPath, "players", "playerinfo.json"))).Where(e => e.Id == user.Id).ToList();

            if (players.Count == 0)
            {
                await this.ReplyAsync("Cannot find game info.");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = user.GetAvatarUrl(),
                    Name = user.Username
                }
            };

            if (game == null)
            {
                foreach (Player player in players)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = player.Game,
                        Value = player.GameInfo
                    });
                }
            }
            else
            {
                Player player = players.Find(e => string.Equals(e.Game, game, StringComparison.CurrentCultureIgnoreCase));

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = player.Game,
                    Value = player.GameInfo
                });
            }

            await this.ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("set")]
        public async Task SetGameInfo(string game, string info)
        {
            Player player = new Player
            {
                Id = this.Context.User.Id,
                Game = game,
                GameInfo = info
            };

            List<Player> players = new List<Player>();

            if (File.Exists(Path.Combine(Program.AppPath, "players", "playerinfo.json")))
            {
                players.AddRange(JsonConvert.DeserializeObject<List<Player>>(File.ReadAllText(Path.Combine(Program.AppPath, "players", "playerinfo.json"))));
            }
            else
            {
                FileHelper.CreateIfDoesNotExist(Program.AppPath, "players", "playerinfo.json");
            }

            players.Add(player);

            string json = JsonConvert.SerializeObject(players);

            File.WriteAllText(Path.Combine(Program.AppPath, "players", "playerinfo.json"), json);

            await this.ReplyAsync("Game info set successfully!");
        }
    }
}
