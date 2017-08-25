using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Deserialization;
using CSharpDewott.ESixOptions;
using CSharpDewott.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using ImageFormat = Discord.ImageFormat;
using IMessage = Discord.IMessage;

namespace CSharpDewott.Commands
{
    public class ImageCommands : ModuleBase
    {
        private readonly string AppPath = Program.AppPath;

        private static Dictionary<ulong, Stopwatch> e6Stopwatches = new Dictionary<ulong, Stopwatch>();

        private static List<string> e621JsonList = new List<string>();

        [Command("bui")]
        public async Task BuiTask()
        {
            string clientId;

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(AppPath, "config.xml"));
            XmlNodeList xmlNodeList = doc.SelectNodes("/Settings/ImgurClientId");
            if (xmlNodeList != null)
            {
                try
                {
                    clientId = xmlNodeList[0].InnerText;
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Imgur client id invalid!\n\nException: " + exception);
                    throw;
                }
            }
            else
            {
                await this.ReplyAsync("Invalid config file!");
                return;
            }

            ImgurClient client = new ImgurClient(clientId);
            AlbumEndpoint endpoint = new AlbumEndpoint(client);
            IAlbum album = await endpoint.GetAlbumAsync("KJTbv");

            int randomInt = Globals.Random.Next(0, album.ImagesCount);

            string imageLink = album.Images.Select(e => e.Link).ToList()[randomInt];

            await this.ReplyAsync(imageLink);
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

        [Command("dewott")]
        public async Task DewottTask()
        {
            string clientId;

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(AppPath, "config.xml"));
            XmlNodeList xmlNodeList = doc.SelectNodes("/Settings/ImgurClientId");
            if (xmlNodeList != null)
            {
                try
                {
                    clientId = xmlNodeList[0].InnerText;
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Imgur client id invalid!\n\nException: " + exception);
                    throw;
                }
            }
            else
            {
                await this.ReplyAsync("Invalid config file!");
                return;
            }

            ImgurClient client = new ImgurClient(clientId);
            AlbumEndpoint endpoint = new AlbumEndpoint(client);
            IAlbum album = await endpoint.GetAlbumAsync("qc266");

            int randomInt = Globals.Random.Next(0, album.ImagesCount);

            string imageLink = album.Images.Select(e => e.Link).ToList()[randomInt];

            await this.ReplyAsync(imageLink);
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
                    await this.ReplyAsync("Invalid image!");
                    return;
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

        [Command("get_wc")]
        public async Task GetWordCloud(IUser user = null)
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

        private async Task GetJson(int pageNumber, List<string> tags)
        {
            string nextE6Json = await Program.Instance.HttpClient.GetStringAsync($"https://e621.net/post/index.json?limit=320&tags={string.Join(" ", tags)}&page={pageNumber}");

            if (nextE6Json == "[]")
            {
                return;
            }

            e621JsonList.Add(nextE6Json.Trim('[', ']'));
        }

        [Command("e6"), Summary("Retrieves an image from e621.net. If the channel is sfw, the command forces the \"rating:safe\" tag to be used. Also remove type tags that are requesting video files")]
        public async Task E6Task(params string[] tags)
        {
            IDisposable typingDisposable = this.Context.Channel.EnterTypingState();

            List<JObject> selectedImages = new List<JObject>();

            JObject currentJObject = null;

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

                for (int i = 1; i < 11; i++)
                {
                    taskList.Add(this.GetJson(i, forcedTags));
                }

                await Task.WhenAll(taskList);

                string e6Json = "[" + string.Join(",", e621JsonList) + "]";

                JArray images = JArray.Parse(e6Json);

                List<JToken> filteredImages = images.ToList();

                filteredImages.RemoveAll(e =>
                {
                    bool shouldDelete = false;

                    if (!this.Context.Channel.IsNsfw && !(this.Context.Channel is IDMChannel) && !(this.Context.Channel is SocketDMChannel) && ((JObject) e).TryGetValue("rating", StringComparison.CurrentCultureIgnoreCase, out JToken resultJToken))
                    {
                        shouldDelete = resultJToken.ToObject<string>() != "s";
                    }

                    if (shouldDelete)
                    {
                        return true;
                    }

                    if (((JObject) e).TryGetValue("file_ext", StringComparison.CurrentCultureIgnoreCase, out JToken resultFileType))
                    {
                        string extension = resultFileType.ToObject<string>();

                        shouldDelete = extension != "png" && extension != "jpg" && extension != "jpeg" && extension != "gif";
                    }

                    if (shouldDelete)
                    {
                        return true;
                    }

                    if (((JObject) e).TryGetValue("tags", StringComparison.CurrentCultureIgnoreCase, out JToken resultTagTokens))
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

                    if (exceededTags != null && ((JObject) e).TryGetValue("tags", StringComparison.CurrentCultureIgnoreCase, out JToken resultTagToken))
                    {
                        return exceededTags.Any(exceededTag => !resultTagToken.ToObject<string>().Split(' ').Any(f => string.Equals(f, exceededTag, StringComparison.CurrentCultureIgnoreCase)));
                    }

                    return false;
                });

                images = new JArray(filteredImages);


                if (images.Count == 0)
                {
                    await this.ReplyAsync("Couldn't find an image with those tags.");
                    return;
                }

                if (getNumberOfImages)
                {
                    await this.ReplyAsync($"Counted {images.Count} images. Please note that this command enforces a limit of query pages, which is then filtered to remove blacklist items and unsupported filetypes.");
                    return;
                }

                requestedNumber = requestedNumber > images.Count ? images.Count : requestedNumber;

                for (int i = 0; i < requestedNumber; i++)
                {
                    int indexOfImage = Globals.Random.Next(0, images.Count);

                    selectedImages.Add((JObject) images[indexOfImage]);

                    images.RemoveAt(indexOfImage);
                }

                foreach (JObject selectedImage in selectedImages)
                {

                    currentJObject = selectedImage;

                    string ext = selectedImage.GetValue("file_ext").ToObject<string>();

                    string url = selectedImage.GetValue("file_url").ToObject<string>() ?? throw new Exception("Couldn't find an image with those tags.");


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

                    System.Drawing.Color embedColor = System.Drawing.Color.Aquamarine;

                    if (selectedImage.TryGetValue("rating", StringComparison.CurrentCultureIgnoreCase, out JToken resultJToken))
                    {
                        switch (resultJToken.ToObject<string>())
                        {
                            case "s":
                            {
                                embedColor = System.Drawing.Color.Green;
                            }
                                break;
                            case "q":
                            {
                                embedColor = System.Drawing.Color.Yellow;
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
                typingDisposable.Dispose();
            }
        }
    }
}
