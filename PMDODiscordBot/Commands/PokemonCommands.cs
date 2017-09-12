// <copyright file="PokemonCommands.cs" company="JordantheBuizel">
// Copyright (c) JordantheBuizel. All rights reserved.
// </copyright>

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using CSharpDewott.Extensions;
using CSharpDewott.PokémonInfo.Items;
using CSharpDewott.PokémonInfo.Moves;
using CSharpDewott.PokémonInfo.Pokémon;
using CSharpDewott.Properties;
using Discord;
using Discord.Commands;
using Humanizer;
using MySql.Data.MySqlClient;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace CSharpDewott.Commands
{
    public class PokemonCommands : ModuleBase
    {
        private readonly string AppPath = Program.AppPath;

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

        [Command("pkinfo"), Summary("Retrieves the requested Pokémon's data from a custom built library"), Alias("poke", "pokeinfo")]
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
                System.Drawing.Image image = null;

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
        }
    }
}
