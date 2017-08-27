using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpDewott.Deserialization;
using Discord;
using Discord.Commands;

namespace CSharpDewott.Commands
{
    public class MarkovCommands : ModuleBase
    {
        private static Dictionary<ulong, Stopwatch> markovStopwatches = new Dictionary<ulong, Stopwatch>();
        
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

        [Command("seedmarkov"), Summary("Creates a sentence using https://github.com/jsvine/markovify/ from seeded sentences in the server.")]
        public async Task SeedMarkov(params string[] seed)
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

                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

                File.WriteAllLines(Path.Combine(Program.AppPath, "Markovs", "currentuser.txt"), allCachedMessages.Values.Select(e => e.Content).Where(e => e.Contains(string.Join(" ", seed))));

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

                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

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

                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

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

                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

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

                Dictionary<ulong, IMessage> allCachedMessages = Program.LogMessages;

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

    }
}
