using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Deserialization;
using CSharpDewott.ESixOptions;
using CSharpDewott.Extensions;
using CSharpDewott.Logging;
using CSharpDewott.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedditSharp;
using RedditSharp.Things;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using IMessage = Discord.IMessage;

namespace CSharpDewott.Commands
{
    public class ImageCommands : ModuleBase
    {
        private readonly string AppPath = Program.AppPath;

        private static Dictionary<ulong, Stopwatch> e6Stopwatches = new Dictionary<ulong, Stopwatch>();

        private static List<ESixImage> e621ImageList = new List<ESixImage>();

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

        [Command("jonbaned")]
        public async Task JonBaned()
        {
            await this.ReplyAsync("https://i.imgur.com/BcJbZXL.gif");
        }

        [Command("bui")]
        public async Task BuiTask()
        {
            ImgurClient client = new ImgurClient(Globals.Settings.ImgurClientId);
            AlbumEndpoint endpoint = new AlbumEndpoint(client);
            IAlbum album = await endpoint.GetAlbumAsync("KJTbv");

            int randomInt = Globals.Random.Next(0, album.ImagesCount);

            string imageLink = album.Images.Select(e => e.Link).ToList()[randomInt];

            await this.ReplyAsync(imageLink);
        }

        [Command("hmmm")]
        public async Task HmmmTask()
        {
            using (this.Context.Channel.EnterTypingState())
            {
                BotWebAgent webAgent = new BotWebAgent(Globals.Settings.RedditUsername, Globals.Settings.RedditPass, Globals.Settings.RedditClientID, Globals.Settings.RedditSecret, "https://github.com/JordanZeotni/CSharpDewott");
                Reddit reddit = new Reddit(webAgent, false);
                Subreddit subreddit = reddit.GetSubreddit("/r/Hmmm/");

                List<Post> posts = subreddit.GetTop((FromTime)Globals.Random.Next(0, 5)).Take(500).Where(e => !e.NSFW).ToList();

                Post selectedPost = posts[Globals.Random.Next(0, posts.Count)];

                Regex linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                string imageLink = selectedPost.Url.ToString();
                if (imageLink.ToLower().Contains("imgur"))
                {
                    ImgurClient client = new ImgurClient(Globals.Settings.ImgurClientId);
                    ImageEndpoint endpoint = new ImageEndpoint(client);
                    imageLink = (await endpoint.GetImageAsync(Regex.Replace(Regex.Replace(imageLink, @".+imgur\.com/", string.Empty), @"\..+", string.Empty))).Link;
                }

                EmbedBuilder builder = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = selectedPost.Title,
                        Url = selectedPost.Shortlink
                    },
                    ImageUrl = imageLink,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Author: {selectedPost.Author.Name}",
                        IconUrl = "https://media.glassdoor.com/sqll/796358/reddit-squarelogo-1490630845152.png"
                    }
                };

                builder.WithColor(Color.OrangeRed);
                
                await this.ReplyAsync(string.Empty, false, builder.Build());
            }
        }

        [Command("bossfight")]
        public async Task BossFight()
        {
            using (this.Context.Channel.EnterTypingState())
            {
                IUserMessage introMessage = await this.ReplyAsync("Wild boss appears! *cue boss music*");

                BotWebAgent webAgent = new BotWebAgent(Globals.Settings.RedditUsername, Globals.Settings.RedditPass, Globals.Settings.RedditClientID, Globals.Settings.RedditSecret, "https://github.com/JordanZeotni/CSharpDewott");
                Reddit reddit = new Reddit(webAgent, false);
                Subreddit subreddit = reddit.GetSubreddit("/r/Bossfight/");

                Post selectedPost = subreddit.GetTop(FromTime.All).Where(e => !e.NSFW).ToList()[Globals.Random.Next(0, subreddit.Posts.Count(e => !e.NSFW))];

                Regex linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                string imageLink = selectedPost.Url.ToString();
                if (imageLink.ToLower().Contains("imgur"))
                {
                    ImgurClient client = new ImgurClient(Globals.Settings.ImgurClientId);
                    ImageEndpoint endpoint = new ImageEndpoint(client);
                    imageLink = (await endpoint.GetImageAsync(Regex.Replace(Regex.Replace(imageLink, @".+imgur\.com/", string.Empty), @"\..+", string.Empty))).Link;
                }

                EmbedBuilder builder = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = selectedPost.Title,
                        Url = selectedPost.Shortlink
                    },
                    ImageUrl = imageLink,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"Author: {selectedPost.Author.Name}",
                        IconUrl = "https://media.glassdoor.com/sqll/796358/reddit-squarelogo-1490630845152.png"
                    }
                };

                builder.WithColor(Color.OrangeRed);

                await introMessage.DeleteAsync();
                await this.ReplyAsync(string.Empty, false, builder.Build());
            }
        }

        [Command("lap"), Alias("lapdance")]
        public async Task LapTask()
        {
            ImgurClient client = new ImgurClient(Globals.Settings.ImgurClientId);
            AlbumEndpoint endpoint = new AlbumEndpoint(client);
            IAlbum album = await endpoint.GetAlbumAsync("rlaNE");

            int randomInt = Globals.Random.Next(0, album.ImagesCount);

            string imageLink = album.Images.Select(e => e.Link).ToList()[randomInt];

            await this.ReplyAsync(imageLink);
        }

        [Command("dewott")]
        public async Task DewottTask()
        {
            ImgurClient client = new ImgurClient(Globals.Settings.ImgurClientId);
            AlbumEndpoint endpoint = new AlbumEndpoint(client);
            IAlbum album = await endpoint.GetAlbumAsync("qc266");

            int randomInt = Globals.Random.Next(0, album.ImagesCount);

            string imageLink = album.Images.Select(e => e.Link).ToList()[randomInt];

            await this.ReplyAsync(imageLink);
        }

        [Command("jonvideo")]
        public async Task JonVideo(params string[] query)
        {
            List<(string Title, string Url)> selectedVideos = LogHandler.JonVideoList.Where(e => query.Length == 0 || e.Title.Split(' ').Any(f => query.Any(g => string.Equals(f, g, StringComparison.CurrentCultureIgnoreCase)))).ToList();

            if (selectedVideos.Any())
            {
                await this.ReplyAsync(selectedVideos[Globals.Random.Next(0, selectedVideos.Count - 1)].Url);
            }
            else
            {
                await this.ReplyAsync("No videos match your search.");
            }
        }

        [Command("randomize")]
        public async Task RandomImage(params string[] imageLink)
        {
            IDisposable typingDisposable = this.Context.Channel.EnterTypingState();

            try
            {
                string imageUrl = imageLink.Length == 0 ? this.Context.User.GetAvatarUrl() : string.Join(" ", imageLink);

                if (!HttpHelper.UrlExists(imageUrl))
                {
                    if ((await this.Context.Guild.GetUsersAsync()).Any(e => string.Equals(e.Username, imageUrl.Split('#')[0], StringComparison.CurrentCultureIgnoreCase) || string.Equals(e.Nickname, imageUrl.Split('#')[0], StringComparison.CurrentCultureIgnoreCase)))
                    {
                        imageUrl = (await this.Context.Guild.GetUsersAsync()).First(e => string.Equals(e.Username, imageUrl, StringComparison.CurrentCultureIgnoreCase) || string.Equals(e.Nickname, imageUrl.Split('#')[0], StringComparison.CurrentCultureIgnoreCase)).GetAvatarUrl();
                    }
                    else if (this.Context.Message.MentionedUserIds.Count > 0)
                    {
                        if (this.Context.Message.MentionedUserIds.Count > 1)
                        {
                            await this.ReplyAsync("Invalid image!");
                            return;
                        }

                        imageUrl = (await this.Context.Channel.GetUserAsync(this.Context.Message.MentionedUserIds.First())).GetAvatarUrl();
                    }
                    else
                    {
                        await this.ReplyAsync("Invalid image!");
                        return;
                    }
                }

                System.Drawing.Image image;

                using (WebClient client = new WebClient())
                using (MemoryStream stream = new MemoryStream())
                {
                    byte[] imageBytes = client.DownloadData(new Uri(imageUrl));

                    await stream.WriteAsync(imageBytes, 0, imageBytes.Length);

                    image = Image.FromStream(stream);
                }

                Bitmap origImage;

                if (image.Width > 450)
                {
                    double ratioX = (double)450 / image.Width;
                    double ratio = ratioX;

                    int width = (int)(image.Width * ratio);
                    int height = (int)(image.Height * ratio);

                    Rectangle destRect = new Rectangle(0, 0, width, height);
                    origImage = new Bitmap(width, height);

                    origImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                    using (Graphics graphics = Graphics.FromImage(origImage))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        using (ImageAttributes wrapMode = new ImageAttributes())
                        {
                            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                        }
                    }
                }
                else
                {
                    origImage = (Bitmap)image;
                }

                Bitmap bmp = new Bitmap(origImage.Width, origImage.Height);

                List<(int x, int y)> remainingCoordsList = new List<(int x, int y)>();

                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        remainingCoordsList.Add((x, y));
                    }
                }

                int originalX = 0;
                int originalY = 0;

                while (remainingCoordsList.Count != 1)
                {
                    int randomInt = Globals.Random.Next(0, remainingCoordsList.Count);

                    bmp.SetPixel(originalX, originalY, origImage.GetPixel(remainingCoordsList[randomInt].x, remainingCoordsList[randomInt].y));

                    remainingCoordsList.RemoveAll(e => e.x == remainingCoordsList[randomInt].x && e.y == remainingCoordsList[randomInt].y);

                    if (originalX == origImage.Width - 1)
                    {
                        if (originalY == origImage.Height - 1)
                        {
                            break;
                        }

                        originalX = 0;
                        originalY++;
                    }
                    else
                    {
                        originalX++;
                    }
                }

                if (File.Exists(Path.Combine(Program.AppPath, "random.png")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "random.png"));
                }

                bmp.Save(Path.Combine(Program.AppPath, "random.png"), System.Drawing.Imaging.ImageFormat.Png);

                await this.Context.Channel.SendFileAsync(Path.Combine(Program.AppPath, "random.png"));

                image.Dispose();
                bmp.Dispose();
                origImage.Dispose();
            }
            catch (Exception exception)
            {
                ConsoleHelper.WriteLine(exception);
            }
            finally
            {
                typingDisposable.Dispose();
            }
        }

        [Command("get_wc"), AdminPrecondition]
        public async Task GetWordCloud(IUser user = null)
        {
            IDisposable typing = this.Context.Channel.EnterTypingState();

            try
            {
                await this.Context.Channel.TriggerTypingAsync();

                if (user == null)
                {
                    user = this.Context.User;
                }

                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

                File.WriteAllLines(Path.Combine(Program.AppPath, "wordcloudinput.txt"), allCachedMessages.Values.Where(e => e.Author.Id == user.Id).Select(e => e.Content));

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3 \"{Path.Combine(Program.AppPath, "Markovs", "wc.py")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process process = Process.Start(info);

                while (process != null && (!File.Exists(Path.Combine(Program.AppPath, "wc.png")) && !process.HasExited))
                {
                }

                if (!File.Exists(Path.Combine(Program.AppPath, "wc.png")))
                {
                    await this.ReplyAsync("Unable to create word cloud.");
                    return;
                }

                await this.Context.Channel.SendFileAsync(Path.Combine(Program.AppPath, "wc.png"));

                File.Delete(Path.Combine(Program.AppPath, "wc.png"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (File.Exists(Path.Combine(Program.AppPath, "wc.png")))
                {
                    File.Delete(Path.Combine(Program.AppPath, "wc.png"));
                }

                typing.Dispose();
            }
        }

        /// <summary>
        /// Splits an IEnumerable&lt;string&gt; to a enumerable list of enumerable lists that can be then used to bypass char limits
        /// </summary>
        /// <param name="listToSplit">IEnumerable&lt;string&gt; that will be split</param>
        /// <param name="charLimit">Limit of characters base split on</param>
        /// <param name="joiningCharCount">Optional joining char count to apply to char counting</param>
        /// <returns></returns>
        private IEnumerable<IEnumerable<string>> SplitToLists(IEnumerable<string> listToSplit, int charLimit, int joiningCharCount = 0)
        {
            List<List<string>> splitLists = new List<List<string>>();

            string joiningCharDummy = string.Empty;

            for (int i = 0; i < joiningCharCount; i++)
            {
                joiningCharDummy += "$";
            }

            IEnumerable<string> toSplit = listToSplit as string[] ?? listToSplit.ToArray();
            if (string.Join(joiningCharDummy, toSplit.Take(toSplit.Count() / 2)).Length < charLimit)
            {
                splitLists.Add(toSplit.Take(toSplit.Count() / 2).ToList());
                splitLists.Add(toSplit.Skip(toSplit.Count() / 2).ToList());
                return splitLists;
            }

            if (string.Join(joiningCharDummy, toSplit.Skip(toSplit.Count() / 2)).Length < charLimit)
            {
                splitLists.AddRange(this.SplitToLists(toSplit.Take(toSplit.Count() / 2), charLimit, joiningCharCount).Select(e => e.ToList()).ToList());
                splitLists.Add(toSplit.Skip(toSplit.Count() / 2).ToList());

                return splitLists;
            }

            splitLists.AddRange(this.SplitToLists(toSplit.Take(toSplit.Count() / 2), charLimit, joiningCharCount).Select(e => e.ToList()).ToList());
            splitLists.AddRange(this.SplitToLists(toSplit.Skip(toSplit.Count() / 2), charLimit, joiningCharCount).Select(e => e.ToList()).ToList());
            return splitLists;
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

        private static async Task GetJson(int pageNumber, IEnumerable<string> tags)
        {
            string nextE6Json = await Program.Instance.HttpClient.GetStringAsync($"https://e621.net/post/index.json?limit=320&tags={string.Join(" ", tags)}&page={pageNumber}");

            if (nextE6Json == "[]")
            {
                return;
            }

            e621ImageList.AddRange(JsonConvert.DeserializeObject<List<ESixImage>>(nextE6Json, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            }));
        }

        [Command("e6"), Summary("Retrieves an image from e621.net. If the channel is sfw, the command forces the \"rating:safe\" tag to be used. Also remove type tags that are requesting video files")]
        public async Task E6Task(params string[] tags)
        {
            IDisposable typingDisposable = this.Context.Channel.EnterTypingState();

            List<ESixImage> selectedImages = new List<ESixImage>();

            ESixImage currentJObject = null;

            Stopwatch e6Stopwatch = null;

            UserOptions options;

            bool getJsonId = tags.Any(e => e.ToLower().Contains("<getid>"));

            try
            {

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
                await this.Context.Channel.TriggerTypingAsync();

                List<string> exceededTags = null;
                if (forcedTags.Count > 6)
                {
                    //await this.ReplyAsync("Tag limit of 6 exceeded.");
                    //return;

                    exceededTags = forcedTags.Skip(6).ToList();
                    forcedTags = forcedTags.Take(6).ToList();
                }

                List<Task> taskList = new List<Task>();

                for (int i = 1; i < 6; i++)
                {
                    taskList.Add(GetJson(i, forcedTags));
                }

                await Task.WhenAll(taskList);

                e621ImageList.RemoveAll(e =>
                {
                    bool shouldDelete = false;

                    if (!this.Context.Channel.IsNsfw)
                    {
                        shouldDelete = e.Rating != ESixImage.E621Rating.Safe;
                    }

                    if (shouldDelete)
                    {
                        return true;
                    }

                    string extension = e.FileExtension;

                    shouldDelete = extension != "png" && extension != "jpg" && extension != "jpeg" && extension != "gif";

                    if (shouldDelete)
                    {
                        return true;
                    }

                    if (e.Tags.Any(f => options.BlackList.Select(g => g.ToLower()).Contains(f.ToLower())))
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

                            return e.Tags.Any(f => string.Equals(f, exceededTag.TrimStart('-'), StringComparison.CurrentCultureIgnoreCase));
                        }

                        return exceededTags.Any(exceededTag => !e.Tags.Any(f => string.Equals(f, exceededTag, StringComparison.CurrentCultureIgnoreCase)));
                    }

                    return false;
                });


                if (e621ImageList.Count == 0)
                {
                    await this.ReplyAsync("Couldn't find an image with those tags.");
                    return;
                }

                if (getNumberOfImages)
                {
                    await this.ReplyAsync($"Counted {e621ImageList.Count} images. Please note that this command enforces a limit of query pages, which is then filtered to remove blacklist items and unsupported filetypes.");
                    return;
                }

                requestedNumber = requestedNumber > e621ImageList.Count ? e621ImageList.Count : requestedNumber;

                for (int i = 0; i < requestedNumber; i++)
                {
                    int indexOfImage = Globals.Random.Next(0, e621ImageList.Count);

                    selectedImages.Add(e621ImageList[indexOfImage]);

                    e621ImageList.RemoveAt(indexOfImage);
                }

                foreach (ESixImage selectedImage in selectedImages)
                {

                    currentJObject = selectedImage;

                    string ext = selectedImage.FileExtension;

                    string url = selectedImage.ImageUrl ?? throw new Exception("Couldn't find an image with those tags.");


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
                        string[] sources = selectedImage.Sources ?? new[] {"No sources have been given for this image."};

                        builder.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Sources",
                            Value = sources.Aggregate((d, g) => $"{d}\n{g}")
                        });
                    }

                    if (options.DisplayTags)
                    {

                        string allTags = string.Join(", ", selectedImage.Tags);
                        List<string> excessTags  = new List<string>();
                        int neededFields = 0;

                        if (allTags.Length > 1024)
                        {
                            excessTags.AddRange(this.SplitToLists(selectedImage.Tags, 1024, 2).Select(enumerable => String.Join(", ", enumerable)));

                            allTags = excessTags.First();
                            
                            excessTags.RemoveAt(0);
                        }

                        builder.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Tags",
                            Value = allTags
                        });

                        foreach (string t in excessTags)
                        {
                            builder.Fields.Add(new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Tags (cont.)",
                                Value = t
                            });
                        }
                    }

                    string artists = selectedImage.Artists != null ? string.Join(", ", selectedImage.Artists) : "Unknown";

                    builder.Author = new EmbedAuthorBuilder
                    {
                        IconUrl = "http://i.imgur.com/3ngaS8h.png",
                        Name = $"#{selectedImage.Id}: {artists}",
                        Url = $"https://e621.net/post/show/{selectedImage.Id}"
                    };

                    builder.ImageUrl = url;

                    builder.Description = $"Score: {selectedImage.Score}\nFavorites: {selectedImage.FavoriteCount}";

                    System.Drawing.Color embedColor = System.Drawing.Color.Aquamarine;

                    switch (selectedImage.Rating)
                    {
                        case ESixImage.E621Rating.Safe:
                        {
                            embedColor = System.Drawing.Color.Green;
                        }
                            break;
                        case ESixImage.E621Rating.Questionable:
                        {
                            embedColor = System.Drawing.Color.Yellow;
                        }
                            break;
                        case ESixImage.E621Rating.Explict:
                        {
                            embedColor = Color.Red;
                        }
                            break;
                        default:
                            break;
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
                    await this.ReplyAsync("Image with id \"" + currentJObject.Id + "\" failed to send.");
                }
            }
            finally
            {
                typingDisposable.Dispose();
                e621ImageList.Clear();
            }
        }
    }

    [Group("e6_option")]
    public class E6Options : ModuleBase
    {
        [Command("set")]
        public async Task E6SetOption(params string[] argStrings)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Program.AppPath, "e6options"));

                UserOptions options = new UserOptions
                {
                    Id = this.Context.User.Id,
                    BlackList = new List<string>
                    {
                        "scat",
                        "gore"
                    },
                    DisplaySources = false,
                    DisplayTags = false,
                    DisplayId = false
                };

                if (File.Exists(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json")))
                {
                    options = JsonConvert.DeserializeObject<UserOptions>(File.ReadAllText(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json")));
                }

                if (argStrings.Length < 2)
                {
                    await this.ReplyAsync("The command setup is `.e6_option set [tags|sources|Id] [true|false]`.\nBy default all options are false.");
                    return;
                }

                switch (argStrings[0].ToLower())
                {
                    case "tags":
                    case "tag":
                    {
                        if (bool.TryParse(argStrings[1], out bool result))
                        {
                            options.DisplayTags = result;
                        }
                        else
                        {
                            await this.ReplyAsync("Option \"Tags\" requires either true or false as the value");
                            return;
                        }
                    }

                        break;
                    case "source":
                    case "sources":
                    {
                        if (bool.TryParse(argStrings[1], out bool result))
                        {
                            options.DisplaySources = result;
                        }
                        else
                        {
                            await this.ReplyAsync("Option \"Sources\" requires either true or false as the value");
                            return;
                        }
                    }
                        break;

                    default:
                    {
                        await this.ReplyAsync("The command setup is `.e6 set [tags|sources|Id] [true|false]` or for blacklists `.e6 set [blacklist add|exclude|blacklist remove|include] [tag].\nBy default all options are false and blacklist contains \"scat\" and \"gore\".");
                        return;
                    }
                }

                File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json"), JsonConvert.SerializeObject(options));

                await this.ReplyAsync("Option set successfully!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("blacklist")]
        public async Task Blacklist(params string[] argStrings)
        {
            Directory.CreateDirectory(Path.Combine(Program.AppPath, "e6options"));

            UserOptions options = new UserOptions
            {
                Id = this.Context.User.Id,
                BlackList = new List<string>
                {
                    "scat",
                    "gore"
                },
                DisplaySources = false,
                DisplayTags = false,
                DisplayId = false
            };

            if (File.Exists(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json")))
            {
                options = JsonConvert.DeserializeObject<UserOptions>(File.ReadAllText(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json")));
            }

            if (argStrings.Length < 1)
            {
                await this.ReplyAsync("The command setup is `.e6_option blacklist [remove|add] [tag(s)]`");
                return;
            }

            if (argStrings[0].ToLower() == "get")
            {
                await this.ReplyAsync($"Your personal blacklist of tags is {string.Join(", ", options.BlackList)}");
                return;
            }

            if (argStrings.Length < 2)
            {
                await this.ReplyAsync("The command setup is `.e6_option blacklist [remove|add] [tag(s)]`");
                return;
            }

            switch (argStrings[0].ToLower())
            {
                case "remove":
                {
                    options.BlackList = options.BlackList.Where(e => !argStrings.Skip(1).Select(f => f.ToLower()).Contains(e.ToLower())).ToList();
                    await this.ReplyAsync($"Option set successfully! Your personal blacklist of tags is now {string.Join(", ", options.BlackList)}");
                }
                    break;
                case "add":
                {
                    options.BlackList.AddRange(argStrings.Skip(1));
                    await this.ReplyAsync($"Option set successfully! Your personal blacklist of tags is now {string.Join(", ", options.BlackList)}");
                }
                    break;
                default:
                {
                    await this.ReplyAsync("The command setup is `.e6_option blacklist [remove|add] [tag(s)]`");
                    return;
                }
            }

            File.WriteAllText(Path.Combine(Program.AppPath, "e6options", $"{this.Context.User.Id}.json"), JsonConvert.SerializeObject(options));
        }
    }
}
