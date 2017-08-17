// <copyright file="Commands.cs" company="JordantheBuizel">
// Copyright (c) JordantheBuizel. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Deserialization;
using CSharpDewott.ESixOptions;
using CSharpDewott.Extensions;
using CSharpDewott.GameInfo;
using CSharpDewott.IO;
using CSharpDewott.Music;
using CSharpDewott.PokémonInfo.Items;
using CSharpDewott.PokémonInfo.Moves;
using CSharpDewott.PokémonInfo.Pokémon;
using CSharpDewott.Preconditions;
using CSharpDewott.Properties;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Scripting.Utils;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp.Extensions;
using Color = System.Drawing.Color;
using unirest_net.http;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace CSharpDewott.Commands
{
    public class Commands : ModuleBase
    {
        private IAudioClient currentAudioClient;
        private Process ffmpeg;
        public readonly string AppPath = Program.AppPath;

        //Cooldowns
        private int lastUsermarkovCall = 0;
        private int lastSongSwitch;

        private static Dictionary<ulong, Stopwatch> markovStopwatches = new Dictionary<ulong, Stopwatch>();

        private static Dictionary<ulong, Stopwatch> e6Stopwatches = new Dictionary<ulong, Stopwatch>();

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

        [Command("2017"), Summary("2017 memes")]
        public async Task Task2017(params string[] input)
        {
            await this.ReplyAsync($">2017\n>{string.Join(" ", input)}");
        }

        [Command("add_blacklist"), Summary("This is a bot owner only command, which means you can't use it ¯\\_(ツ)_/¯"), AdminPrecondition]
        public async Task BlacklistAdd(string commandName)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Program.AppPath, "blacklists"));

                List<string> blacklists = new List<string> {this.Context.Channel.Id.ToString()};

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

        [Command("get_wc")]
        public async Task GetWordCloud(IUser user = null)
        {
            await this.Context.Channel.TriggerTypingAsync();

            if (user == null)
            {
                user = this.Context.User;
            }

            Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

            foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)).Where(e => !File.ReadAllLines(Path.Combine(Program.AppPath, "markov-blacklists", "blacklists.txt")).Any(e.Contains)))
            {
                allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }));
            }

            File.WriteAllLines(Path.Combine(Program.AppPath, "wordcloudinput.txt"), allCachedMessages.Values.Where(e => e.Author.Id == user.Id).Select(e => e.Content));

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "py",
                Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "wc.py")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            Process markovProcess = Process.Start(info);

            string output = markovProcess.StandardOutput.ReadToEnd();

            Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

            while (!File.Exists(Path.Combine(Program.AppPath, "wc.png")))
            {
            }
            
            await this.Context.Channel.SendFileAsync(Path.Combine(Program.AppPath, "wc.png"));

            File.Delete(Path.Combine(Program.AppPath, "wc.png"));
        }

        
        public async Task E6FileSizeChecker(JArray images, params string[] tags)
        {
            try
            {
                if (images.Count(e =>
                {
                    if ((JObject)e != null && ((JObject)e).TryGetValue("file_size", StringComparison.CurrentCultureIgnoreCase, out JToken result))
                    {
                        return result.ToObject<ulong>() > 8000000;
                    }
                    return false;
                }) == images.Count)
                {
                    await this.ReplyAsync("All images with that tag are greater than 8MB.");
                    return;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine(ex);

                return;
            }

            await this.E6Task(tags);
        }

        [Command("e6"), Summary("Retrieves an image from e621.net. If the channel is sfw, the command forces the \"rating:safe\" tag to be used. Also remove type tags that are requesting video files")]
        public async Task E6Task(params string[] tags)
        {
            List<JObject> selectedImages = new List<JObject>();

            JObject currentJObject = null;

            await this.StopTyping();

            Stopwatch e6Stopwatch = null;
            
            UserOptions options;

            if (!File.Exists(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json")))
            {
                options = new UserOptions
                {
                    Id = this.Context.User.Id,
                    DisplaySources = false,
                    DisplayTags = false,
                    BlackList = new List<string>
                    {
                        "scat",
                        "gore"
                    }
                };

                File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json"), JsonConvert.SerializeObject(options));
            }
            else
            {
                options = JsonConvert.DeserializeObject<UserOptions>(File.ReadAllText(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json")));
            }

            if (!this.Context.Channel.Name.ToLower().Contains("bot") && !(this.Context.Channel is IDMChannel) && !(this.Context.Channel is SocketDMChannel) && this.Context.Channel.Id != 344718149398036490)
            {

                if (e6Stopwatches.TryGetValue(this.Context.Channel.Id, out e6Stopwatch) && e6Stopwatch.IsRunning && e6Stopwatch.ElapsedMilliseconds < 200000)
                {
                    await this.ReplyAsync($"Please wait {200 - e6Stopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!e6Stopwatches.ContainsKey(this.Context.Channel.Id))
                {
                    e6Stopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    e6Stopwatches.TryGetValue(this.Context.Channel.Id, out e6Stopwatch);
                }
            }

            bool getNumberOfImages = tags.Any(e => e.ToLower().Contains("<getcount>"));

            bool getJsonId = tags.Any(e => e.ToLower().Contains("<getid>"));

            int requestedNumber = 1;

            List<string> forcedTags = tags.ToList();

            foreach (string forcedTag in forcedTags)
            {
                if (int.TryParse(forcedTag, out int result))
                {
                    if (this.Context.Channel.Name.ToLower().Contains("bot") || this.Context.Channel.Id == 344718149398036490)
                    {
                        requestedNumber = result <= 5 ? result : 5;
                    }
                    else
                    {
                        await this.ReplyAsync("Keep image spam to bot channels. The image count has been set to 1.");
                    }
                }
            }

            forcedTags.RemoveAll(e => e.ToLower() == "<getcount>" || e.ToLower() == "<getid>" || int.TryParse(e, out int _));

            try
            {
                await this.Context.Channel.TriggerTypingAsync();

                List<string> exceededTags = null;
                if (forcedTags.Count > 6)
                {
                    //await this.ReplyAsync("Tag limit of 6 exceeded.");
                    //return;

                    exceededTags = forcedTags.Skip(6).ToList();
                    forcedTags = forcedTags.Take(6).ToList();
                }

                string url = $"https://e621.net/post/index.json?limit=320&tags={string.Join(" ", forcedTags)}";

                string e6Json = (await Program.Instance.HttpClient.GetStringAsync(url)).TrimEnd(']');

                string nextE6Json = string.Empty;

                int page = 2;

                while (nextE6Json == string.Empty || Regex.Matches(nextE6Json, "id:").Count >= 320)
                {
                    nextE6Json = await Program.Instance.HttpClient.GetStringAsync(url + $"&page={page}");

                    if (nextE6Json == "[]")
                    {
                        break;
                    }

                    e6Json += "," + nextE6Json.TrimStart('[').TrimEnd(']');

                    page++;
                }

                e6Json += "]";

                JArray images = JArray.Parse(e6Json);

                List<JToken> filteredImages = images.ToList();

                filteredImages.RemoveAll(e =>
                {
                    bool shouldDelete = false;

                    if (!this.Context.Channel.IsNsfw && !(this.Context.Channel is IDMChannel) && !(this.Context.Channel is SocketDMChannel) && ((JObject)e).TryGetValue("rating", StringComparison.CurrentCultureIgnoreCase, out JToken resultJToken))
                    {
                        shouldDelete = resultJToken.ToObject<string>() != "s";
                    }

                    if (shouldDelete)
                    {
                        return true;
                    }

                    if (((JObject)e).TryGetValue("file_ext", StringComparison.CurrentCultureIgnoreCase, out JToken resultFileType))
                    {
                        string extension = resultFileType.ToObject<string>();

                        shouldDelete = extension != "png" && extension != "jpg" && extension != "jpeg" && extension != "gif";
                    }
                    
                    if (shouldDelete)
                    {
                        return true;
                    }

                    if (((JObject)e).TryGetValue("tags", StringComparison.CurrentCultureIgnoreCase, out JToken resultTagTokens))
                    {
                        if (resultTagTokens.ToObject<string>().Split(' ').Any(f => options.BlackList.Select(g => g.ToLower()).Contains(f.ToLower())))
                        {
                            return true;
                        }

                        if (exceededTags != null)
                        {
                            foreach (string exceededTag in exceededTags)
                            {
                                if (!exceededTag.StartsWith("-"))
                                {
                                    continue;
                                }

                                return resultTagTokens.ToObject<string>().Split(' ').Any(f => string.Equals(f, exceededTag.TrimStart('-'), StringComparison.CurrentCultureIgnoreCase));
                            }
                        }
                    }

                    if (exceededTags != null && ((JObject)e).TryGetValue("tags", StringComparison.CurrentCultureIgnoreCase, out JToken resultTagToken))
                    {
                        return exceededTags.Any(exceededTag => !resultTagToken.ToObject<string>().Split(' ').Any(f => string.Equals(f, exceededTag, StringComparison.CurrentCultureIgnoreCase)));
                    }

                    return false;
                });

                images = new JArray(filteredImages.ToList());


                if (images.Count == 0)
                {
                    await this.ReplyAsync("Couldn't find an image with those tags.");
                    return;
                }

                if (getNumberOfImages)
                {
                    await this.ReplyAsync($"Counted {images.Count} images. Please note that this command enforces a limit of 640 posts, which is then filtered to remove blacklist items and unsupported filetypes.");
                    return;
                }

                requestedNumber = requestedNumber > images.Count ? images.Count : requestedNumber;

                for (int i = 0; i < requestedNumber; i++)
                {
                    int indexOfImage = Globals.Random.Next(0, images.Count);

                    selectedImages.Add((JObject)images[indexOfImage]);

                    images.RemoveAt(indexOfImage);
                }

                foreach (JObject selectedImage in selectedImages)
                {

                    currentJObject = selectedImage;

                    string ext = selectedImage.GetValue("file_ext").ToObject<string>();

                    url = selectedImage.GetValue("file_url").ToObject<string>() ?? throw new Exception("Couldn't find an image with those tags.");


                    //                    if (selectedImage.GetValue("file_size").ToObject<ulong>() > 8000000)
                    //                    {
                    //                        await this.E6FileSizeChecker(images, tags);
                    //                        return;
                    //                    }

                    //Do mimetypes
                    switch (ext)
                    {
                        case "jpg":
                        case "jpeg":
                        case "gif":
                        case "png":
                            break;
                        case "webm":
                            {
                                await this.E6Task(tags);
                                return;
                            }
                        case "mp4":
                            {
                                await this.E6Task(tags);
                                return;
                            }
                        default:
                            {
                                await this.E6Task(tags);
                                return;
                            }
                    }

                    //                    using (MemoryStream stream = new MemoryStream(await Program.Instance.HttpClient.GetByteArrayAsync(url)))
                    //                    {
                    //                        await this.Context.Channel.SendFileAsync(stream, $"image.{ext}");

                    EmbedBuilder builder = new EmbedBuilder();

                    if (options.DisplaySources)
                    {
                        string[] sources;

                        if (selectedImage.TryGetValue("sources", StringComparison.CurrentCultureIgnoreCase, out JToken result))
                        {
                            sources = result.ToObject<string[]>();
                        }
                        else
                        {
                            sources = new[]
                            {
                                    "No sources have been given for this image."
                            };
                        }

                        builder.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Sources",
                            Value = sources.Aggregate((d, g) => $"{d}\n{g}")
                        });
                    }

                    if (options.DisplayTags)
                    {

                        string allTags = selectedImage.GetValue("tags").ToObject<string>();

                        if (allTags.Length > 2048)
                        {
                            string truncatedTags = string.Empty;

                            string[] tagsList = allTags.Split(' ');

                            foreach (string s in tagsList)
                            {
                                if (truncatedTags.Length < 2000)
                                {
                                    truncatedTags += s + ", ";
                                    continue;
                                }

                                truncatedTags += s;
                                break;
                            }

                            allTags = truncatedTags;
                        }

                        builder.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Tags",
                            Value = allTags
                        });
                    }

                    string artists = selectedImage.TryGetValue("artist", StringComparison.CurrentCultureIgnoreCase, out JToken authorToken) ? string.Join(", ", authorToken.ToObject<string[]>()) : "Unknown";

                    builder.Author = new EmbedAuthorBuilder
                    {
                        IconUrl = "http://i.imgur.com/3ngaS8h.png",
                        Name = $"#{selectedImage.GetValue("id").ToObject<string>()}: {artists}",
                        Url = $"https://e621.net/post/show/{selectedImage.GetValue("id").ToObject<string>()}"
                    };

                    builder.ImageUrl = url;

                    builder.Description = $"Score: {selectedImage.GetValue("score").ToObject<string>()}\nFavorites: {selectedImage.GetValue("fav_count").ToObject<string>()}";

                    Color embedColor = Color.Aquamarine;

                    if (selectedImage.TryGetValue("rating", StringComparison.CurrentCultureIgnoreCase, out JToken resultJToken))
                    {
                        switch (resultJToken.ToObject<string>())
                        {
                            case "s":
                                {
                                    embedColor = Color.Green;
                                }
                                break;
                            case "q":
                                {
                                    embedColor = Color.Yellow;
                                }
                                break;
                            case "e":
                                {
                                    embedColor = Color.Red;
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    builder.WithColor(embedColor);

                    await this.ReplyAsync(string.Empty, false, builder.Build());
                }

                e6Stopwatch?.Restart();
                //}
            }
            catch (Exception e)
            {
                ConsoleHelper.WriteLine(e);

                if (currentJObject == null)
                {
                    await this.ReplyAsync("JObject was null!");
                    return;
                }

                if (getJsonId)
                {
                    await this.ReplyAsync("Image with id \"" + currentJObject.GetValue("id").ToObject<string>() + "\" failed to send.");
                }
            }
            finally
            {
                await this.StopTyping();
            }
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
        public async Task Roll(int max)
        {
            if (max < 0)
            {
                max = Math.Abs(max);
            }

            await this.ReplyAsync($"Rolled {Globals.Random.Next(1, max)}");
        }

        [Command("lewdmarkov"), Summary("Creates a sentence using https://github.com/jsvine/markovify/ from the a couple lemons. Innocent lemons. 🤔")]
        public async Task LewdMarkov()
        {
            try
            {
                Stopwatch markovstopwatch = null;

                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovstopwatch) && markovstopwatch.IsRunning && markovstopwatch.ElapsedMilliseconds < 60000)
                {
                    await this.ReplyAsync($"Please wait {60 - markovstopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!markovStopwatches.ContainsKey(this.Context.Channel.Id) && !this.Context.Channel.Name.Contains("bot"))
                {
                    markovStopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovstopwatch);
                }

                File.WriteAllText(Path.Combine(Program.AppPath, "Markovs", "fileLoc.txt"), "lewdmarkov.txt");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "markovSentence.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process markovProcess = Process.Start(info);

                string output = markovProcess.StandardOutput.ReadToEnd();

                Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

                while (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                }

                if (string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
                else
                {
                    await this.ReplyAsync(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt")).Replace("@", string.Empty).Replace("\n", string.Empty));

                    markovstopwatch?.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")) || string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "Markovs", "output.txt"));
                }
            }
        }

        [Command("doctormarkov"), Summary("Creates a sentence using https://github.com/jsvine/markovify/ from the script of the Dr. Who Episode \"Don't Blink\".")]
        public async Task DoctorMarkov()
        {
            try
            {
                Stopwatch markovStopwatch = null;

                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch) && markovStopwatch.IsRunning && markovStopwatch.ElapsedMilliseconds < 60000)
                {
                    await this.ReplyAsync($"Please wait {60 - markovStopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!markovStopwatches.ContainsKey(this.Context.Channel.Id) && !this.Context.Channel.Name.Contains("bot"))
                {
                    markovStopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch);
                }

                File.WriteAllText(Path.Combine(Program.AppPath, "Markovs", "fileLoc.txt"), "dontblink.txt");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "markovNewline.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process markovProcess = Process.Start(info);

                string output = markovProcess.StandardOutput.ReadToEnd();

                Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

                while (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                }

                if (string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
                else
                {
                    await this.ReplyAsync(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt")));

                    markovStopwatch?.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")) || string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "Markovs", "output.txt"));
                }
            }
        }

        [Command("servermarkov"), Summary("Creates a sentence using https://github.com/jsvine/markovify/ from all sentences in the server.")]
        public async Task ServerMarkov()
        {
            try
            {
                Stopwatch markovStopwatch = null;

                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch) && markovStopwatch.IsRunning && markovStopwatch.ElapsedMilliseconds < 60000)
                {
                    await this.ReplyAsync($"Please wait {60 - markovStopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!markovStopwatches.ContainsKey(this.Context.Channel.Id) && !this.Context.Channel.Name.Contains("bot"))
                {
                    markovStopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch);
                }

                if (!Directory.Exists(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)))
                {
                    return;
                }

                Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

                foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)).Where(e => !File.ReadAllLines(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt")).Any(e.Contains)))
                {
                    allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }

                File.WriteAllLines(Path.Combine(Program.AppPath, "Markovs", "currentuser.txt"), allCachedMessages.Values.Select(e => e.Content));

                File.WriteAllText(Path.Combine(Program.AppPath, "Markovs", "fileLoc.txt"), "currentuser.txt");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "markovNewline.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process markovProcess = Process.Start(info);

                string output = markovProcess?.StandardOutput.ReadToEnd();

                Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

                while (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                }

                if (string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
                else
                {
                    await this.ReplyAsync(Regex.Replace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt")), @"<@[^\s]*([0-9]+)>", e =>
                    {
                        IGuildUser user = this.Context.Guild.GetUserAsync(Convert.ToUInt64(e.Value.Replace("@", string.Empty).Replace("!", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty))).Result;

                        return string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname;
                    }));

                    markovStopwatch?.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")) || string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "Markovs", "output.txt"));
                }
            }
        }

        [Command("uwusermarkov")]
        [Summary("Creates a sentence using https://github.com/jsvine/markovify/ from sentences you've said before.")]
        public async Task UwuserMarkov(IUser user = null)
        {
            try
            {
                Stopwatch markovStopwatch = null;

                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch) && markovStopwatch.IsRunning && markovStopwatch.ElapsedMilliseconds < 60000)
                {
                    await this.ReplyAsync($"Please wait {60 - markovStopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!markovStopwatches.ContainsKey(this.Context.Channel.Id) && !this.Context.Channel.Name.Contains("bot"))
                {
                    markovStopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch);
                }

                if (user == null)
                {
                    user = this.Context.User;
                }

                if (!Directory.Exists(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)))
                {
                    return;
                }

                Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

                foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)).Where(e => !File.ReadAllLines(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt")).Any(e.Contains)))
                {
                    allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }

                allCachedMessages = allCachedMessages.Where(e => e.Value.Author.Id == user.Id).ToDictionary(e => e.Key, e => e.Value);

                File.WriteAllLines(Path.Combine(Program.AppPath, "Markovs", "currentuser.txt"), allCachedMessages.Values.Select(e => e.Content));

                File.WriteAllText(Path.Combine(Program.AppPath, "Markovs", "fileLoc.txt"), "currentuser.txt");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "markovNewline.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process markovProcess = Process.Start(info);

                string output = markovProcess?.StandardOutput.ReadToEnd();

                Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

                while (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                }

                if (string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
                else
                {
                    await this.ReplyAsync(Regex.Replace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt")), @"<@[^\s]*([0-9]+)>", e =>
                    {
                        IGuildUser founduser = this.Context.Guild.GetUserAsync(Convert.ToUInt64(e.Value.Replace("@", string.Empty).Replace("!", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty))).Result;

                        return string.IsNullOrWhiteSpace(founduser.Nickname) ? $"[{founduser.Username}]" : $"[{founduser.Nickname}]";
                    }).Replace("u", "uwu"));

                    markovStopwatch?.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")) || string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "Markovs", "output.txt"));
                }
            }
        }

        [Command("usermarkov")]
        [Summary("Creates a sentence using https://github.com/jsvine/markovify/ from sentences you've said before.")]
        public async Task UserMarkov(IUser user = null)
        {
            try
            {
                Stopwatch markovStopwatch = null;

                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch) && markovStopwatch.IsRunning && markovStopwatch.ElapsedMilliseconds < 60000)
                {
                    await this.ReplyAsync($"Please wait {60 - markovStopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!markovStopwatches.ContainsKey(this.Context.Channel.Id) && !this.Context.Channel.Name.Contains("bot"))
                {
                    markovStopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch);
                }

                if (user == null)
                {
                    user = this.Context.User;
                }

                if (!Directory.Exists(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)))
                {
                    return;
                }

                Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

                foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)).Where(e => !File.ReadAllLines(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt")).Any(e.Contains)))
                {
                    allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }

                allCachedMessages = allCachedMessages.Where(e => e.Value.Author.Id == user.Id).ToDictionary(e => e.Key, e => e.Value);

                File.WriteAllLines(Path.Combine(Program.AppPath, "Markovs", "currentuser.txt"), allCachedMessages.Values.Select(e => e.Content));

                File.WriteAllText(Path.Combine(Program.AppPath, "Markovs", "fileLoc.txt"), "currentuser.txt");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "markovNewline.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process markovProcess = Process.Start(info);

                string output = markovProcess?.StandardOutput.ReadToEnd();

                Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

                while (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                }

                if (string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
                else
                {
                    await this.ReplyAsync(Regex.Replace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt")), @"<@[^\s]*([0-9]+)>", e =>
                    {
                        IGuildUser founduser = this.Context.Guild.GetUserAsync(Convert.ToUInt64(e.Value.Replace("@", string.Empty).Replace("!", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty))).Result;

                        return string.IsNullOrWhiteSpace(founduser.Nickname) ? $"[{founduser.Username}]" : $"[{founduser.Nickname}]";
                    }));

                    markovStopwatch?.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")) || string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "Markovs", "output.txt"));
                }
            }
        }

        [Command("multimarkov")]
        [Summary("Creates a sentence using https://github.com/jsvine/markovify/ from sentences you and whoever else requested have said before.")]
        public async Task MultiMarkov(params IUser[] users)
        {
            try
            {
                List<ulong> requestedUsersIds = users.Select(e => e.Id).ToList();
                requestedUsersIds.Add(this.Context.User.Id);

                Stopwatch markovStopwatch = null;

                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch) && markovStopwatch.IsRunning && markovStopwatch.ElapsedMilliseconds < 60000)
                {
                    await this.ReplyAsync($"Please wait {60 - markovStopwatch.Elapsed.Seconds} seconds until using this command.");
                    return;
                }

                if (!markovStopwatches.ContainsKey(this.Context.Channel.Id) && !this.Context.Channel.Name.Contains("bot"))
                {
                    markovStopwatches.Add(this.Context.Channel.Id, new Stopwatch());

                    markovStopwatches.TryGetValue(this.Context.Channel.Id, out markovStopwatch);
                }

                if (!Directory.Exists(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)))
                {
                    return;
                }

                Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

                foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)).Where(e => !File.ReadAllLines(Path.Combine(Program.AppPath, "markov-blacklists", $"blacklists.txt")).Any(e.Contains)))
                {
                    allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                }

                allCachedMessages = allCachedMessages.Where(e => users.Any(f => f.Id == e.Value.Author.Id)).ToDictionary(e => e.Key, e => e.Value);

                File.WriteAllLines(Path.Combine(Program.AppPath, "Markovs", "currentmulti.txt"), allCachedMessages.Values.Select(e => e.Content));

                File.WriteAllText(Path.Combine(Program.AppPath, "Markovs", "fileLoc.txt"), "currentmulti.txt");

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "markovNewline.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process markovProcess = Process.Start(info);

                string output = markovProcess?.StandardOutput.ReadToEnd();

                Console.Out.WriteLine($"[python-output@{DateTime.Now.ToLongTimeString()}]: {output}");

                while (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                }

                if (string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
                else
                {
                    await this.ReplyAsync(Regex.Replace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt")), @"<@[^\s]*([0-9]+)>", e =>
                    {
                        IGuildUser user = this.Context.Guild.GetUserAsync(Convert.ToUInt64(e.Value.Replace("@", string.Empty).Replace("!", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty))).Result;

                        return string.IsNullOrWhiteSpace(user.Nickname) ? $"[{user.Username}]" : $"[{user.Nickname}]";
                    }));

                    markovStopwatch?.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (!File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")) || string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(Program.AppPath, "Markovs", "output.txt"))))
                {
                    await this.ReplyAsync("Markov creation failed!");
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "Markovs", "output.txt")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "Markovs", "output.txt"));
                }
            }
        }

        [Command("makemehappy")]
        public async Task CheerUpTask(string filter = null)
        {
            if (!this.Context.Channel.IsNsfw)
            {
                return;
            }

            List<string> happyList = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Bots", "Pokémon"), "*", SearchOption.AllDirectories).SelectMany(Directory.GetFiles).ToList();

            int luckyIndex = Globals.Random.Next(0, happyList.Count - 1);


            using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(happyList[luckyIndex])))
            {
                await this.Context.Channel.SendFileAsync(stream, $"{new FileInfo(happyList[luckyIndex]).Directory.Name}{Path.GetExtension(happyList[luckyIndex])}");
            }
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

        [Command("setgame"), AdminPrecondition, Summary("Sets the \"Playing\" text")]
        public async Task SetGameText(string gametext)
        {
            await Program.Client.SetGameAsync(gametext);
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

        [Command("timer")]
        public async Task TimerTest()
        {
            /*if (markovStopwatch.IsRunning)
            {
                if (!this.Context.Channel.Name.Contains("bot") && markovStopwatch.ElapsedMilliseconds < 200000)
                {
                    await this.ReplyAsync("*insert wait text here*");

                    return;
                }

                await this.ReplyAsync("Restarting...");

                return;
            }

            markovStopwatch.Start();

            await this.ReplyAsync("Starting...");*/
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

        /*[Command("serverstatus")]
        [Alias("server")]
        public async Task ServerStatus()
        {
            if (this.Context.User.Id == 228019100008316948 || this.Context.User.Id == 213135389861478400)
            {
                Globals.TypingDisposable = this.Context.Channel.EnterTypingState();

                List<IGuildUser> peopleToMention = (await this.Context.Guild.GetUsersAsync()).Where(user => user.Username == "JordantheBuizel" || user.Username == "kiki").ToList();

                bool ftpUp = true;

                FtpWebRequest request1 = (FtpWebRequest)WebRequest.Create("ftp://localhost/test.txt");
                request1.Credentials = new NetworkCredential("anonymous", string.Empty);
                request1.Method = WebRequestMethods.Ftp.GetDateTimestamp;

                try
                {
                    FtpWebResponse response1 = (FtpWebResponse)request1.GetResponse();
                }
                catch (WebException)
                {
                    ftpUp = false;
                }

                if (ftpUp)
                {
                    await this.Context.Channel.SendMessageAsync("Ftp is up!");
                }
                else
                {
                    await this.Context.Channel.SendMessageAsync(
                        $"{peopleToMention[0].Mention} {peopleToMention[1].Mention} Ftp is down!!!");
                }

                /*bool ServerUp = true;
                Tcp.TcpClient tcpClient = new Tcp.TcpClient();#1#
                string hostName = "localhost";
                /*try
                {

                    tcpClient.CustomHeaderSize = 3;

                    if (tcpClient.SocketState == Tcp.TcpSocketState.Idle)
                    {
                        tcpClient.Connect(hostName, 4001);
                    }
                }
                catch (Exception ex)
                {
                    ServerUp = false;
                }

                if (ServerUp == true && tcpClient.Socket.Connected)
                    await Context.Channel.SendMessageAsync("Game server is up!");

                else
                    await Context.Channel.SendMessageAsync("@JordantheBuizel#8352 @kiki#7066 Game server is down!!!");

                tcpClient.Close();#1#

                bool sQLUp = true;
                string cs = @"server=" + hostName + @";userid=jordan;database=pmu_data;password=JordantheBuizel;";

                MySqlConnection conn = null;

                try
                {
                    conn = new MySqlConnection(cs);
                    conn.Open();

                    string stm = "SELECT VERSION()";
                    MySqlCommand cmd = new MySqlCommand(stm, conn);
                    string version = Convert.ToString(cmd.ExecuteScalar());
                    Console.WriteLine("MySQL version : {0}", version);
                }
                catch
                {
                    sQLUp = false;
                }

                if (sQLUp)
                {
                    await this.Context.Channel.SendMessageAsync("SQL server is up!");
                }
                else
                {
                    await this.Context.Channel.SendMessageAsync(
                        $"{peopleToMention[0].Mention} {peopleToMention[1].Mention} SQL server is down!!!");
                }

                Globals.TypingDisposable.Dispose();
            }
        }*/

        [Summary("Displays the requested Pokémon's portrait.")]
        [Command("portrait")]
        public async Task Portrait(
            [Summary("Pokemon name. Any pokemon that have \"'\", spaces, or \".\" will have to be removed. e.g Mr. Mime => MrMime.")]
            string poke,
            [Summary("Optionally choose the portrait number (Note: C# indexes start with 0)\nSome Pokémon have only three portraits so also keep that in mind.")]
            int index = 0)
        {
            string indexText = index.ToWords();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(Program.AppPath, "Portraits.dat"));
                using (MemoryStream ms =
                    new MemoryStream(Convert.FromBase64String(doc.SelectNodes($"//{poke}/{indexText}")[0]
                        .InnerText)))
                {
                    await this.Context.Channel.SendFileAsync(ms, $"{poke}.png");
                }
            }
            catch (Exception ex)
            {
                await this.Context.Channel.SendMessageAsync($"Invalid entry! Remember, Poke is case sensitive.\n{ex.Message}");
            }
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

        [Summary("Gets info about a requested move"), Command("move")]
        public async Task Move(
            [Summary("Move to search.")]
            params string[] item)
        {
            string requestedMove = string.Join(" ", item);

            if (MoveList.AllMoves == null)
            {
                MoveHelper.InitializeMoves();
            }

            if (MoveList.AllMoves == null)
            {
                return;
            }

            Move move = MoveList.AllMoves.FirstOrDefault(e => string.Equals(e.Id, requestedMove, StringComparison.CurrentCultureIgnoreCase) || string.Equals(e.Name, requestedMove, StringComparison.CurrentCultureIgnoreCase));

            if (move == null)
            {
                await this.ReplyAsync("Move not found!");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder
            {
                Title = move.Name
            };

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Description",
                Value = move.Discription
            });

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Accuracy",
                Value = move.Accuracy == -1 ? "Never Misses" : $"{move.Accuracy}%"
            });

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Base Power",
                Value = move.BasePower
            });

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "PP",
                Value = move.PP
            });

            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Category",
                Value = Enum.GetName(typeof(Move.MoveCategory), move.Category)
            });
            
            builder.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Priority",
                Value = move.Priority
            });

            if (move.ContestType != null)
            {
                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Contest Type",
                    Value = move.ContestType
                });
            }

            builder.Description = move.Flags.ToString();

            builder.WithColor(move.TypeColor);

            await this.ReplyAsync(string.Empty, false, builder.Build());
        }

        [Summary("Gets item from multiple sources")]
        [Command("item")]
        public async Task Item(
            [Summary("Item to search. Must be in quotes")]
            params string[] itemStrings)
        {
            string item = string.Join(" ", itemStrings);

            if (ItemList.AllItems == null)
            {
                ItemHelper.InitializeItems();
            }

            if (ItemList.AllItems == null)
            {
                return;
            }

            if (ItemList.AllItems.Any(e => string.Equals(e.Id, item, StringComparison.CurrentCultureIgnoreCase) || string.Equals(e.Name, item, StringComparison.CurrentCultureIgnoreCase)))
            {
                Item requestedItem = ItemList.AllItems.First(e => string.Equals(e.Id, item, StringComparison.CurrentCultureIgnoreCase) || string.Equals(e.Name, item, StringComparison.CurrentCultureIgnoreCase));

                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = requestedItem.Name,
                    Description = requestedItem.Description,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"#{requestedItem.ItemNum}"
                    }
                };

                if (requestedItem.Fling != null)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Fling Move stats",
                        Value = $"Base Power: {requestedItem.Fling.BasePower}"
                    });
                }

                if (requestedItem.Generation != null)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "First added in:",
                        Value = requestedItem.Generation.GetName()
                    });
                }

                if (requestedItem.NaturalGiftInfo != null)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Natural Gift Move stats",
                        Value = $"Base Power: {requestedItem.NaturalGiftInfo.BasePower}\nType: {requestedItem.NaturalGiftInfo.MoveType}"
                    });
                }

                if (requestedItem.MegaEvolves != null)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Mega Evolves:",
                        Value = requestedItem.MegaEvolves
                    });
                }

                if (requestedItem.MegaStone != null)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Mega Form:",
                        Value = requestedItem.MegaStone
                    });
                }

                builder.Footer.IconUrl = $"https://raw.githubusercontent.com/110Percent/beheeyem-data/master/sprites/items/{requestedItem.Name.ToLower().Replace(" ", "-").Replace("'", string.Empty)}.png"; // https://raw.githubusercontent.com/110Percent/beheeyem-data/master/sprites/items/" + item.name.toLowerCase().replace(" ", "-").replace("'", "") + ".png"

                await this.ReplyAsync(string.Empty, false, builder.Build());

                return;
            }

            string hostName = "localhost";

            string cs = @"server=" + hostName + @";userid=jordan;database=pmu_data;password=JordantheBuizel;";

            MySqlConnection conn = null;

            try
            {
                conn = new MySqlConnection(cs);
                conn.Open();

                string stm = "SELECT VERSION()";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                string version = Convert.ToString(cmd.ExecuteScalar());
                Console.WriteLine("MySQL version : {0}", version);
            }
            catch (Exception exception)
            {
                ConsoleHelper.WriteLine(exception);

                IUser jordan = await this.Context.Client.GetUserAsync(228019100008316948);
                IDMChannel jordanDmChannel = await jordan.GetOrCreateDMChannelAsync();
                await jordanDmChannel.SendMessageAsync("SQL is down!");
                return;
            }

            try
            {
                EmbedBuilder builder = new EmbedBuilder();

                string itemName = item;

                string info = string.Empty;
                int picNum = -1;

                itemName = itemName.Replace("\'", "\'\'").Replace("\\", string.Empty);
                itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(itemName);

                builder.Title = itemName;

                MySqlCommand command = new MySqlCommand($"SELECT info, pic FROM `item` WHERE name = '{itemName}'", conn);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    info = reader.GetString("info");
                    picNum = reader.GetInt32("pic");
                }

                if (info == string.Empty || info.ToLower() == "empty" || picNum == -1)
                {
                    await this.Context.Channel.SendMessageAsync("Item not found!");
                    return;
                }

                reader.Close();

                using (Bitmap bmp = new Bitmap(32, 32))
                using (Graphics gra = Graphics.FromImage(bmp))
                {
                    int y = picNum / 6 * 32;
                    int x = Math.Abs((int)(((double)picNum / 6 - y) * 6 * 32));

                    gra.DrawImage(Resources.Items, new Rectangle(0, 0, 32, 32), new Rectangle(x, y, 32, 32), GraphicsUnit.Pixel);

                    if (Directory.Exists($"C:\\Abyss Web Server\\htdocs\\") && !File.Exists($"C:\\Abyss Web Server\\htdocs\\{itemName.Replace(" ", "_")}.png"))
                    {
                        bmp.Save($"C:\\Abyss Web Server\\htdocs\\{itemName.Replace(" ", "_")}.png", ImageFormat.Png);
                    }
                }

                builder.ThumbnailUrl = $"http://209.141.45.22/{itemName.Replace(" ", "_")}.png";
                builder.Description = info;

                await this.Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine(ex);
            }
        }

        [Command("stats"), Summary("Gets random info about the server."), Alias("stat", "serverinfo")]
        public async Task Stats()
        {
            Dictionary<ulong, DeserializedMessage> allCachedMessages = new Dictionary<ulong, DeserializedMessage>();

            foreach (string file in Directory.GetFiles(Path.Combine(Program.AppPath, "Logs", this.Context.Guild.Name)))
            {
                allCachedMessages = allCachedMessages.AddRange(JsonConvert.DeserializeObject<Dictionary<ulong, DeserializedMessage>>(File.ReadAllText(file), new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }));
            }

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

        [Command("r34proof")]
        public async Task ProofTask()
        {
            return;

            /*List<string> PokemonNames = Constants.AllPokemonNamesList.Select(e => e.Replace(" ", "_").Replace(":", string.Empty).ToLower()).ToList();

            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "I can't believe you are this naive"
            };

            int completed = 0;

            foreach (string pokemonName in PokemonNames)
            {
                await this.Context.Channel.TriggerTypingAsync();

                string url = $"https://e621.net/post/index.json?limit=320&tags={pokemonName} rating:e";

                string e6Json = await Program.Instance.HttpClient.GetStringAsync(url);

                JArray images = JArray.Parse(e6Json);

                completed++;

                if (800 % completed == 0)
                {
                    await this.ReplyAsync($"{(double)completed / 800 * 100}% complete.");
                }

                if (images.Count == 0)
                {
                    await (await this.Context.User.GetOrCreateDMChannelAsync()).SendMessageAsync($"{pokemonName}");
                    continue;
                }

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = pokemonName,
                    Value = $"Counted {images.Count} explict images."
                });
            }*/

            //await this.ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("why")]
        public async Task Why()
        {
            await this.ReplyAsync("https://www.youtube.com/watch?v=XW5Lmq4mwQs");
        }
        
        [Command("pkinfo"), Summary("Retrieves the requested Pokémon's data from a customly built library"), Alias("poke", "pokeinfo")]
        public async Task PokemonInfo(
            [Summary("Pokémon's name to search. Must be in quotes if there are spaces.")]
            params string[] pokeNameStrings)
        {
            string pokeName = string.Join(" ", pokeNameStrings);

            await this.Context.Channel.TriggerTypingAsync();

            string parsedImageUrl = string.Empty;

            try
            {

                if (PokéDex.InstanceDex == null)
                {
                    new PokéDex();
                }

                if (PokéDex.InstanceDex == null)
                {
                    Console.WriteLine("[Command 'pokeinfo']: Instance is still null!");
                    return;
                }

                Pokémon requestedPokémon = PokéDex.InstanceDex.AllPokémon.FirstOrDefault(e => string.Equals(e.SpeciesName, pokeName, StringComparison.CurrentCultureIgnoreCase) || int.TryParse(pokeName, out int result) && e.DexNum == result);

                if (requestedPokémon == null)
                {
                    await this.ReplyAsync($"Pokémon '{pokeName}' not found! {(pokeName.ToLower() == "nidoran" ? "\nNidoran Requires '-F' or '-M' to indicate gender." : string.Empty)}");
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = requestedPokémon.SpeciesName,
                    Footer = new EmbedFooterBuilder { Text = $"#{requestedPokémon.DexNum}" }
                };

                parsedImageUrl = $"https://play.pokemonshowdown.com/sprites/xyani/{requestedPokémon.SpeciesName}.gif";

                parsedImageUrl = parsedImageUrl.Replace("Mime Jr.", "mimejr").Replace("Mr. Mime", "mrmime").Replace("Type: Null", "typenull").Replace("Nidoran-F", "nidoranf").Replace("Nidoran-M", "nidoranm").Replace("Ho-Oh", "hooh").Replace("Hakamo-o", "hakamoo").Replace("Kammo-o", "kammoo").Replace("Porygon-Z", "porygonz").Replace("Zygarde-10%", "zygarde-10").ToLower();
                
                // Currently has issues
                // builder.ThumbnailUrl = parsedImageUrl;

                // So we do this instead :^)
                Image image = null;

                if (HttpHelper.UrlExists(parsedImageUrl))
                {
                    byte[] imageBytes = await Program.Instance.HttpClient.GetByteArrayAsync(parsedImageUrl);

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

                    await this.Context.Channel.SendFileAsync(Path.Combine(this.AppPath, "pokemon.gif"));
                }

                // Back to your regularlly scheduled builder
                builder.WithColor(requestedPokémon.Color.Name == "Brown" ? Color.FromArgb(40, 26, 13) : requestedPokémon.Color);

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Prevolution",
                    Value = requestedPokémon.EvolvesFrom == null ? "No prevolution" : string.Join(", ", requestedPokémon.EvolvesFrom)
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Evolutions",
                    Value = requestedPokémon.EvolvesInto == null ? "No evolutions" : string.Join(", ", requestedPokémon.EvolvesInto)
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "Types",
                    Value = requestedPokémon.Types == null ? "Error" : string.Join(", ", requestedPokémon.Types)
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Size",
                    Value = requestedPokémon.Height
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Mass",
                    Value = requestedPokémon.Weight
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Egg Groups",
                    Value = $"{requestedPokémon.EggGroups.EggGroup1}{(requestedPokémon.EggGroups.EggGroup2 == null ? string.Empty : "\n" + requestedPokémon.EggGroups.EggGroup2)}"
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "Base stats",
                    Value = $"HP: {requestedPokémon.BaseStats.Hp} ATK: {requestedPokémon.BaseStats.Attack} DEF: {requestedPokémon.BaseStats.Defense} SPATK: {requestedPokémon.BaseStats.SpecialAttack} SPDEF: {requestedPokémon.BaseStats.SpecialDefense} SPE: {requestedPokémon.BaseStats.Speed}"
                });

                builder.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Abilities",
                    Value = $"{requestedPokémon.Abilities.Ability1}{(requestedPokémon.Abilities.Ability2 == requestedPokémon.Abilities.Ability1 || requestedPokémon.Abilities.Ability2 == null ? string.Empty : $"; {requestedPokémon.Abilities.Ability2}")}"
                });

                if (requestedPokémon.Abilities.AbilityH != null)
                {
                    builder.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Hidden Ability",
                        Value = requestedPokémon.Abilities.AbilityH
                    });
                }

                await this.ReplyAsync(string.Empty, false, builder.Build());

                /*SvnClient client = new SvnClient();
                client.Export(SvnTarget.FromString("https://github.com/Zarel/Pokemon-Showdown/trunk/master/data"), PathsHelper.CreateIfDoesNotExist(Program.AppPath, "Pokemon-Data"));



                string hostName = "localhost";

                string cs = @"server=" + hostName + @";userid=jordan;database=pmu_data;password=JordantheBuizel;";

                MySqlConnection conn = null;

                try
                {
                    conn = new MySqlConnection(cs);
                    conn.Open();

                    string stm = "SELECT VERSION()";
                    MySqlCommand cmd = new MySqlCommand(stm, conn);
                    string version = Convert.ToString(cmd.ExecuteScalar());
                    Console.WriteLine("MySQL version : {0}", version);
                }
                catch
                {
                    IUser jordan = await this.Context.Client.GetUserAsync(228019100008316948);
                    IDMChannel jordanDmChannel = await jordan.GetOrCreateDMChannelAsync();
                    await jordanDmChannel.SendMessageAsync("SQL is down!");
                    return;
                }

                try
                {
                    int dexNum = 0;

                    string eggGroup1 = string.Empty;

                    string eggGroup2 = string.Empty;

                    bool validPoke = false;

                    bool specificForm = pokeName.Contains("-") &&
                                        pokeName.Split('-')[1].Length > 3;

                    pokeName = !specificForm
                        ? pokeName
                        : pokeName.Split('-')[0];

                    string formName = specificForm ? pokeName.Split('-')[1] : string.Empty;

                    EmbedBuilder messageBuilder = new EmbedBuilder();

                    int hP = 0;

                    int attack = 0;

                    int defense = 0;

                    int specialAttack = 0;

                    int specialDefense = 0;

                    int speed = 0;

                    double height = 0;

                    double weight = 0;

                    List<string> abilities = new List<string>();

                    Program.PokemonType type1 = Program.PokemonType.None;

                    Program.PokemonType type2 = Program.PokemonType.None;

                    pokeName = pokeName.Replace("\'", "\'\'").Replace("\\", string.Empty);
                    formName = formName.Replace("\'", "\'\'").Replace("\\", string.Empty);

                    pokeName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pokeName);

                    List<string> forms = new List<string>();

                    MySqlCommand command = new MySqlCommand(
                        $"SELECT DexNum FROM `pokedex_pokemon` WHERE PokemonName = '{pokeName}'",
                        conn);
                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        validPoke = reader.HasRows;
                        dexNum = reader.GetInt32("DexNum");
                    }

                    if (dexNum > 7000)
                    {
                        dexNum -= 7000;
                    }

                    if (dexNum > 721 || dexNum <= 0)
                    {
                        await this.Context.Channel.SendMessageAsync("Pokémon not found!");
                        return;
                    }

                    reader.Close();

                    messageBuilder.Title = pokeName;
                    messageBuilder.Footer = new EmbedFooterBuilder().WithText($"#{dexNum}");

                    command = new MySqlCommand(
                        $"SELECT EggGroup1, EggGroup2 FROM `pokedex_pokemon` WHERE PokemonName = '{pokeName}'",
                        conn);
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        eggGroup1 = reader.GetString("EggGroup1");
                        eggGroup2 = reader.GetString("EggGroup2");
                    }

                    reader.Close();

                    command = new MySqlCommand(
                        $"SELECT EggGroup1, EggGroup2 FROM `pokedex_pokemon` WHERE PokemonName = '{pokeName}'",
                        conn);
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        eggGroup1 = reader.GetString("EggGroup1");
                        eggGroup2 = reader.GetString("EggGroup2");
                    }

                    reader.Close();

                    if (!eggGroup2.Contains("Undiscovered"))
                    {
                        eggGroup2 = ", " + eggGroup2;
                    }
                    else
                    {
                        eggGroup2 = string.Empty;
                    }

                    messageBuilder.Fields.Add(new EmbedFieldBuilder
                    {
                        Name = $"Egg Groups:",
                        Value = $"{eggGroup1}{eggGroup2}",
                        IsInline = false
                    });

                    if (!specificForm)
                    {
                        command = new MySqlCommand(
                            $"SELECT FormName FROM `pokedex_pokemonform` WHERE DexNum = '{dexNum}'", conn);
                        reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            forms.Add(reader.GetString("FormName"));
                        }

                        reader.Close();

                        List<string> distinctForms = forms.Distinct().ToList();

                        if (distinctForms.Count > 1)
                        {
                            string formsBuilder = string.Empty;

                            foreach (string form in distinctForms)
                            {
                                formsBuilder += " " + pokeName + "-" + form;
                            }

                            messageBuilder.Fields.Add(new EmbedFieldBuilder
                            {
                                Name = "Other forms:",
                                Value = formsBuilder,
                                IsInline = true
                            });
                        }

                        command = new MySqlCommand(
                            $"SELECT HP, Attack, Defense, SpecialAttack, SpecialDefense, Speed, Height, Weight, Type1, Type2, Ability1, Ability2, Ability3 FROM `pokedex_pokemonform` WHERE DexNum = '{dexNum}' AND FormName = 'Normal'",
                            conn);
                        reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            hP = reader.GetInt32("HP");
                            attack = reader.GetInt32("Attack");
                            defense = reader.GetInt32("Defense");
                            specialAttack = reader.GetInt32("SpecialAttack");
                            specialDefense = reader.GetInt32("SpecialDefense");
                            speed = reader.GetInt32("Speed");
                            height = reader.GetDouble("Height");
                            weight = reader.GetDouble("Weight");
                            type1 = (Program.PokemonType)reader.GetInt32("Type1");
                            type2 = (Program.PokemonType)reader.GetInt32("Type2");
                            abilities.Add(reader.GetString("Ability1"));
                            abilities.Add(reader.GetString("Ability2"));
                            abilities.Add(reader.GetString("Ability3"));
                        }

                        reader.Close();

                        List<string> distinctAbilities = abilities.Distinct().ToList();
                        distinctAbilities.RemoveAll(e => e.Contains("None"));

                        messageBuilder.Fields.Add(new EmbedFieldBuilder().WithName("\nAbilities: ").WithIsInline(false));

                        if (distinctAbilities.Count == 1)
                        {
                            messageBuilder.Fields.Last().Value += distinctAbilities[0];
                        }
                        else
                        {
                            foreach (string ability in abilities)
                            {
                                if (!((string)messageBuilder.Fields.Last().Value).Contains(ability) && ability != "None")
                                {
                                    messageBuilder.Fields.Last().Value += ability + ", ";
                                }
                            }

                            messageBuilder.Fields.Last().Value = ((string)messageBuilder.Fields.Last().Value).TrimEnd(' ', ',');
                        }

                        messageBuilder.Fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Base stats:",
                            Value = $"{hP}/{attack}/{defense}/{specialAttack}/{specialDefense}/{speed}",
                            IsInline = false
                        });

                        if (type2 != Program.PokemonType.None)
                        {
                            messageBuilder.Fields.Add(new EmbedFieldBuilder
                            {
                                Name = "Types:",
                                Value = $"{Enum.GetName(typeof(Program.PokemonType), type1)}, {Enum.GetName(typeof(Program.PokemonType), type2)}",
                                IsInline = false
                            });
                        }
                        else
                        {
                            messageBuilder.Fields.Add(new EmbedFieldBuilder
                            {
                                Name = "Types:",
                                Value = $"{Enum.GetName(typeof(Program.PokemonType), type1)}",
                                IsInline = false
                            });
                        }
                        messageBuilder.Fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Height:",
                            Value = $"{height}",
                            IsInline = false
                        });

                        messageBuilder.Fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Weight:",
                            Value = $"{weight}",
                            IsInline = true
                        });

                        await this.StopTyping();

                        await this.Context.Channel.SendMessageAsync(string.Empty, false, messageBuilder.Build());
                    }
                    else
                    {
                        command = new MySqlCommand(
                            $"SELECT HP, Attack, Defense, SpecialAttack, SpecialDefense, Speed, Height, Weight, Type1, Type2, Ability1, Ability2, Ability3 FROM `pokedex_pokemonform` WHERE DexNum = '{dexNum}' AND FormName = '{formName}'",
                            conn);
                        reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            hP = reader.GetInt32("HP");
                            attack = reader.GetInt32("Attack");
                            defense = reader.GetInt32("Defense");
                            specialAttack = reader.GetInt32("SpecialAttack");
                            specialDefense = reader.GetInt32("SpecialDefense");
                            speed = reader.GetInt32("Speed");
                            height = reader.GetDouble("Height");
                            weight = reader.GetDouble("Weight");
                            type1 = (Program.PokemonType)reader.GetInt32("Type1");
                            type2 = (Program.PokemonType)reader.GetInt32("Type2");
                            abilities.Add(reader.GetString("Ability1"));
                            abilities.Add(reader.GetString("Ability2"));
                            abilities.Add(reader.GetString("Ability3"));
                        }

                        reader.Close();

                        List<string> distinctAbilities = abilities.Distinct().ToList();
                        distinctAbilities.RemoveAll(e => e.Contains("None"));

                        messageBuilder.Fields.Add(new EmbedFieldBuilder().WithName("\nAbilities: ").WithIsInline(false));

                        if (distinctAbilities.Count == 1)
                        {
                            messageBuilder.Fields.Last().Value += distinctAbilities[0];
                        }
                        else
                        {
                            foreach (string ability in abilities)
                            {
                                if (!((string)messageBuilder.Fields.Last().Value).Contains(ability) && ability != "None")
                                {
                                    messageBuilder.Fields.Last().Value += ability + ", ";
                                }
                            }

                            messageBuilder.Fields.Last().Value = ((string)messageBuilder.Fields.Last().Value).TrimEnd(' ', ',');
                        }

                        messageBuilder.Fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Base stats:",
                            Value = $"{hP}/{attack}/{defense}/{specialAttack}/{specialDefense}/{speed}",
                            IsInline = false
                        });

                        if (type2 != Program.PokemonType.None)
                        {
                            messageBuilder.Fields.Add(new EmbedFieldBuilder
                            {
                                Name = "Types:",
                                Value = $"{Enum.GetName(typeof(Program.PokemonType), type1)}, {Enum.GetName(typeof(Program.PokemonType), type2)}",
                                IsInline = false
                            });
                        }
                        else
                        {
                            messageBuilder.Fields.Add(new EmbedFieldBuilder
                            {
                                Name = "Types:",
                                Value = $"{Enum.GetName(typeof(Program.PokemonType), type1)}",
                                IsInline = false
                            });
                        }
                        messageBuilder.Fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Height:",
                            Value = $"{height}",
                            IsInline = false
                        });

                        messageBuilder.Fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Weight:",
                            Value = $"{weight}",
                            IsInline = true
                        });

                        await this.StopTyping();

                        await this.Context.Channel.SendMessageAsync(string.Empty, false, messageBuilder.Build());
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine($"Error \n {ex.Message} \n {ex.StackTrace} \n {ex.Source}");
                }*/
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"Error \n {ex.Message} \n {ex.StackTrace} \n {ex.Source}");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(parsedImageUrl);
                request.Method = WebRequestMethods.Http.Head;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await this.ReplyAsync($"Sprite <{parsedImageUrl}> does not exist!");
                }
            }

            await this.StopTyping();
        }


        /*[Summary("Gets the requested pokemon randomly from google images. Warning: If you didn't know already, Google Images can be very... scary sometimes.")]
        [Command("pokeimage")]
        [Alias("pkimg")]
        public async Task PokeImage(
            [Summary("Pokémon's name to search. Must be in quotes")]
            string pokeName)
        {
            Globals.TypingDisposable = this.Context.Channel.EnterTypingState();

            pokeName = pokeName.Replace("\'", "\'\'").Replace("\\", string.Empty).ToLower();

            Console.Out.WriteLine("Test");

            PokemonSpecies p;
            try
            {
                p = await DataFetcher.GetNamedApiObject<PokemonSpecies>(pokeName);
            }
            catch (Exception ex)
            {
                p = null;
            }

            if (p == null)
            {
                await this.Context.Channel.SendMessageAsync("Pokémon not found!");
                return;
            }

            int dexNum = p.ID;

            if (dexNum == 502)
            {
                await this.Context.Channel.SendMessageAsync("OH! THATS ME! Hold on let me get a good one...");

                Thread.Sleep(2500);

                await this.Context.Channel.SendMessageAsync(
                    "Hmm.. this is perfect image! Now I have to make sure I get it right. After all it is me~");

                Thread.Sleep(2500);
            }

            try
            {
                string html = this.GetHtmlCode(pokeName);

                List<string> urls = this.GetUrls(html);
                Random rnd = new Random();

                int randomUrl = rnd.Next(0, 5);

                string luckyUrl = urls[randomUrl];

                byte[] image = this.GetImage(luckyUrl);
                using (MemoryStream ms = new MemoryStream(image))
                {
                    Image imageResult = Image.FromStream(ms);

                    if (dexNum == 502)
                    {
                        imageResult.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }

                    using (MemoryStream str = new MemoryStream())
                    {
                        imageResult.Save(str, ImageFormat.Png);
                        str.Position = 0;
                        await this.Context.Channel.SendFileAsync(
                            str,
                            "poke.png");
                    }

                    if (dexNum == 502)
                    {
                        await this.Context.Channel.SendMessageAsync("Oh NO! What have I done?");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"Error \n {ex.Message} \n {ex.StackTrace} \n {ex.Source}");
            }

           Globals.TypingDisposable?.Dispose();
        }

        private bool HasStringNone(string text)
        {
            return text.ToLower() == "None";
        }

        private string GetHtmlCode(string query)
        {
            string url = "https://www.google.com/search?q=" + query + "&tbm=isch";
            string data = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (Stream dataStream = response.GetResponseStream())
            {
                if (dataStream == null)
                {
                    return string.Empty;
                }

                using (StreamReader sr = new StreamReader(dataStream))
                {
                    data = sr.ReadToEnd();
                }
            }

            return data;
        }

        private List<string> GetUrls(string html)
        {
            List<string> urls = new List<string>();
            int ndx = html.IndexOf("class=\"images_table\"", StringComparison.Ordinal);
            ndx = html.IndexOf("<img", ndx, StringComparison.Ordinal);

            while (ndx >= 0)
            {
                ndx = html.IndexOf("src=\"", ndx, StringComparison.Ordinal);
                ndx = ndx + 5;
                int ndx2 = html.IndexOf("\"", ndx, StringComparison.Ordinal);
                string url = html.Substring(ndx, ndx2 - ndx);
                urls.Add(url);
                ndx = html.IndexOf("<img", ndx, StringComparison.Ordinal);
            }

            return urls;
        }

        private byte[] GetImage(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (Stream dataStream = response.GetResponseStream())
            {
                if (dataStream == null)
                {
                    return null;
                }

                using (BinaryReader sr = new BinaryReader(dataStream))
                {
                    byte[] bytes = sr.ReadBytes(100000);

                    return bytes;
                }
            }
        }*/

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
