// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NumberGame.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the NumberGame type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CSharpDewott.Commands.Modules
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using CSharpDewott.Extensions;
    using CSharpDewott.GameInfo;
    using CSharpDewott.IO;

    using Discord;
    using Discord.Commands;

    using Newtonsoft.Json;

    public class NumberGame
    {

        /// <summary>
        /// The current guesses.
        /// </summary>
        private int currentGuesses;

        /// <summary>
        /// The correct number.
        /// </summary>
        private int correctNumber;

        /// <summary>
        /// Gets or sets the channel id.
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        public ICommandContext Context { get; set; }

        /// <summary>
        /// Gets or sets the player user.
        /// </summary>
        public IUser PlayerUser { get; set; }

        /// <summary>
        /// Gets or sets the difficulty.
        /// </summary>
        public int Difficulty { get; set; }

        /// <summary>
        /// Gets a value indicating whether the game is over.
        /// </summary>
        public bool IsGameOver { get; private set; }

        public NumberGame(IUser playerOne, int difficulty, ICommandContext context)
        {
            this.ChannelId = context.Channel.Id;
            this.PlayerUser = playerOne;
            this.Difficulty = difficulty;
            this.Context = context;

            this.BeginGame(difficulty).GetAwaiter().GetResult();
        }

        public async Task GuessNumber(int guess)
        {
            IUserMessage message = this.Context.Message;

            if (guess > this.correctNumber)
            {
                this.currentGuesses++;
                await message.Channel.SendMessageAsync($"Too high! You have guessed {this.currentGuesses} times.");
            }

            if (guess < this.correctNumber)
            {
                this.currentGuesses++;
                await message.Channel.SendMessageAsync($"Too low! You have guessed {this.currentGuesses} times.");
            }

            if (guess == this.correctNumber)
            {
                this.currentGuesses++;
                await message.Channel.SendMessageAsync(
                    $"Congrats! You have guessed {this.currentGuesses} times to get the correct number, {this.correctNumber}.");

                if (!File.Exists(Path.Combine(Program.AppPath, "numbergame", $"record{this.Difficulty}.json")))
                {
                    FileHelper.CreateIfDoesNotExist(Program.AppPath, "numbergame", $"record{this.Difficulty}.json");

                    NGRecords records = new NGRecords
                    {
                        Difficulty = this.Difficulty,
                        Guesses = this.currentGuesses,
                        Id = message.Author.Id
                    };

                    string json = JsonConvert.SerializeObject(records);

                    File.WriteAllText(Path.Combine(Program.AppPath, "numbergame", $"record{this.Difficulty}.json"), json);
                }
                else
                {
                    NGRecords records = new NGRecords
                    {
                        Difficulty = this.Difficulty,
                        Guesses = this.currentGuesses,
                        Id = message.Author.Id
                    };

                    NGRecords oldRecords = JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(Path.Combine(Program.AppPath, "numbergame", $"record{this.Difficulty}.json")));

                    if (oldRecords.Guesses > this.currentGuesses)
                    {
                        string json = JsonConvert.SerializeObject(records);

                        File.WriteAllText(Path.Combine(Program.AppPath, "numbergame", $"record{this.Difficulty}.json"), json);
                    }
                }

                NGRecords newestRecords = JsonConvert.DeserializeObject<NGRecords>(File.ReadAllText(Path.Combine(Program.AppPath, "numbergame", $"record{this.Difficulty}.json")));

                await message.Channel.SendMessageAsync($"The current record is {newestRecords.Guesses}, set by {((ITextChannel)message.Channel).Guild.GetUserAsync(newestRecords.Id).Result.Username}");

                this.IsGameOver = true;
            }
        }

        private async Task BeginGame(int difficulty)
        {
            using (IDisposable typeDisposable = this.Context.Channel.EnterTypingState())
            {
                try
                {
                    switch (difficulty)
                    {
                        case 1:
                        {
                            await this.Context.Channel.SendMessageAsync("Starting number game with easy difficulty...");
                            this.correctNumber = Program.Random.Next(1, 100);
                            await this.Context.Channel.SendMessageAsync(
                                "Begin guessing! The number is between 1 and 100");
                        }

                            break;
                        case 2:
                        {
                            await this.Context.Channel.SendMessageAsync("Starting number game with medium difficulty...");
                            this.correctNumber = Program.Random.Next(1, 1000);
                                await this.Context.Channel.SendMessageAsync(
                                "Begin guessing! The number is between 1 and 1000");
                        }

                            break;
                        case 3:
                        {
                            await this.Context.Channel.SendMessageAsync("Starting number game with hard difficulty...");
                            this.correctNumber = Program.Random.Next(1, 10000);
                                await this.Context.Channel.SendMessageAsync(
                                "Begin guessing! The number is between 1 and 10000");
                        }

                            break;
                        case 4:
                        {
                            await this.Context.Channel.SendMessageAsync("Starting number game with extreme difficulty...");
                            this.correctNumber = Program.Random.Next(1, 100000);
                                await this.Context.Channel.SendMessageAsync(
                                "Begin guessing! The number is between 1 and 100000");
                        }

                            break;
                        default:
                        {
                            await this.Context.Channel.SendMessageAsync("Starting number game with easy difficulty...");
                            this.correctNumber = Program.Random.Next(1, 100);
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
        }
    }
}
