using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CSharpDewott.Deserialization.Converters;
using CSharpDewott.PokémonInfo.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CSharpDewott.PokémonInfo.Moves
{
    public class Move
    {
        public enum MoveCategory
        {
            Physical,
            Special,
            Status
        }

        private string type;

        [JsonProperty("num")]
        public int ItemNum { get; set; }
        [JsonProperty("accuracy"), JsonConverter(typeof(AccuracyConverter))]
        public int Accuracy { get; set; }
        [JsonProperty("basePower")]
        public int BasePower { get; set; }
        [JsonProperty("category"), JsonConverter(typeof(StringEnumConverter))]
        public MoveCategory Category { get; set; }
        [JsonProperty("desc")]
        public string Discription { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("pp")]
        public int PP { get; set; }
        [JsonProperty("priority")]
        public int Priority { get; set; }
        [JsonProperty("flags")]
        public Flags Flags { get; set; }
        [JsonProperty("type")]
        public string Type
        {
            get => this.type;
            set
            {
                this.type = value;
                this.CalculateColorFromType(value);
            }
        }
        [JsonProperty("contestType")]
        public string ContestType { get; set; }
        public Color TypeColor { get; set; }

        private void CalculateColorFromType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "normal":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#A8A878");
                        return;
                    }
                case "fire":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#F08030");
                        return;
                    }
                case "fighting":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#C03028");
                        return;
                    }
                case "water":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#6890F0");
                        return;
                    }
                case "flying":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#A890F0");
                        return;
                    }
                case "grass":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#78C850");
                        return;
                    }
                case "poison":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#A040A0");
                        return;
                    }
                case "electric":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#F8D030");
                        return;
                    }
                case "ground":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#E0C068");
                        return;
                    }
                case "psychic":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#F85888");
                        return;
                    }
                case "rock":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#B8A038");
                        return;
                    }
                case "ice":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#98D8D8");
                        return;
                    }
                case "bug":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#A8B820");
                        return;
                    }
                case "dragon":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#7038F8");
                        return;
                    }
                case "ghost":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#705898");
                        return;
                    }
                case "dark":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#705848");
                        return;
                    }
                case "steel":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#B8B8D0");
                        return;
                    }
                case "fairy":
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#EE99AC");
                        return;
                    }
                default:
                    {
                        this.TypeColor = ColorTranslator.FromHtml("#68A090");
                        return;
                    }
            }
        }
    }

    public class Flags
    {
        [JsonProperty("authentic"), JsonConverter(typeof(BoolConverter)), Flag("Ignores a target's substitute.")]
        public bool Authentic { get; set; }
        [JsonProperty("bite"), JsonConverter(typeof(BoolConverter)), Flag("Power is multiplied by 1.5 when used by a Pokemon with the Ability Strong Jaw.")]
        public bool Bite { get; set; }
        [JsonProperty("bullet"), JsonConverter(typeof(BoolConverter)), Flag("Has no effect on Pokemon with the Ability Bulletproof.")]
        public bool Bullet { get; set; }
        [JsonProperty("charge"), JsonConverter(typeof(BoolConverter)), Flag("The user is unable to make a move between turns.")]
        public bool Charge { get; set; }
        [JsonProperty("contact"), JsonConverter(typeof(BoolConverter)), Flag("Makes contact.")]
        public bool Contact { get; set; }
        [JsonProperty("dance"), JsonConverter(typeof(BoolConverter)), Flag("When used by a Pokemon, other Pokemon with the Ability Dancer can attempt to execute the same move.")]
        public bool Dance { get; set; }
        [JsonProperty("defrost"), JsonConverter(typeof(BoolConverter)), Flag("Thaws the user if executed successfully while the user is frozen.")]
        public bool Defrost { get; set; }
        [JsonProperty("distance"), JsonConverter(typeof(BoolConverter)), Flag("Can target a Pokemon positioned anywhere in a Triple Battle.")]
        public bool Distance { get; set; }
        [JsonProperty("gravity"), JsonConverter(typeof(BoolConverter)), Flag("Prevented from being executed or selected during Gravity's effect.")]
        public bool Gravity { get; set; }
        [JsonProperty("heal"), JsonConverter(typeof(BoolConverter)), Flag("Prevented from being executed or selected during Heal Block's effect.")]
        public bool Heal { get; set; }
        [JsonProperty("mirror"), JsonConverter(typeof(BoolConverter)), Flag("Can be copied by Mirror Move.")]
        public bool Mirror { get; set; }
        [JsonProperty("mystery"), JsonConverter(typeof(BoolConverter)), Flag("Unknown effect.")]
        public bool Mystery { get; set; }
        [JsonProperty("nonsky"), JsonConverter(typeof(BoolConverter)), Flag("Prevented from being executed or selected in a Sky Battle.")]
        public bool Nonsky { get; set; }
        [JsonProperty("powder"), JsonConverter(typeof(BoolConverter)), Flag("Has no effect on Grass-type Pokemon, Pokemon with the Ability Overcoat, and Pokemon holding Safety Goggles.")]
        public bool Powder { get; set; }
        [JsonProperty("protect"), JsonConverter(typeof(BoolConverter)), Flag("Blocked by Detect, Protect, Spiky Shield, and if not a Status move, King's Shield.")]
        public bool Protect { get; set; }
        [JsonProperty("pulse"), JsonConverter(typeof(BoolConverter)), Flag("Power is multiplied by 1.5 when used by a Pokemon with the Ability Mega Launcher.")]
        public bool Pulse { get; set; }
        [JsonProperty("punch"), JsonConverter(typeof(BoolConverter)), Flag("Power is multiplied by 1.2 when used by a Pokemon with the Ability Iron Fist.")]
        public bool Punch { get; set; }
        [JsonProperty("recharge"), JsonConverter(typeof(BoolConverter)), Flag("If this move is successful, the user must recharge on the following turn and cannot make a move.")]
        public bool Recharge { get; set; }
        [JsonProperty("reflectable"), JsonConverter(typeof(BoolConverter)), Flag("Bounced back to the original user by Magic Coat or the Ability Magic Bounce.")]
        public bool Reflectable { get; set; }
        [JsonProperty("snatch"), JsonConverter(typeof(BoolConverter)), Flag("Can be stolen from the original user and instead used by another Pokemon using Snatch.")]
        public bool Snatch { get; set; }
        [JsonProperty("sound"), JsonConverter(typeof(BoolConverter)), Flag("Has no effect on Pokemon with the Ability Soundproof.")]
        public bool Sound { get; set; }

        public override string ToString()
        {
            string desc = string.Empty;
            
            foreach (PropertyInfo property in typeof(Flags).GetProperties())
            {
                if (this.Authentic)
                {

                    if (property.Name == "Authentic")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Bite)
                {

                    if (property.Name == "Bite")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Bullet)
                {

                    if (property.Name == "Bullet")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Charge)
                {

                    if (property.Name == "Charge")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Contact)
                {

                    if (property.Name == "Contact")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Dance)
                {

                    if (property.Name == "Dance")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Defrost)
                {

                    if (property.Name == "Defrost")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Distance)
                {

                    if (property.Name == "Distance")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Gravity)
                {

                    if (property.Name == "Gravity")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Heal)
                {

                    if (property.Name == "Heal")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Mirror)
                {

                    if (property.Name == "Mirror")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Mystery)
                {

                    if (property.Name == "Mystery")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Nonsky)
                {

                    if (property.Name == "Nonsky")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Powder)
                {

                    if (property.Name == "Powder")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Protect)
                {

                    if (property.Name == "Protect")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Pulse)
                {

                    if (property.Name == "Pulse")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Punch)
                {

                    if (property.Name == "Punch")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Recharge)
                {

                    if (property.Name == "Recharge")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Reflectable)
                {

                    if (property.Name == "Reflectable")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Snatch)
                {

                    if (property.Name == "Snatch")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
                if (this.Sound)
                {

                    if (property.Name == "Sound")
                    {
                        desc += $"{((FlagAttribute)property.GetCustomAttributes(typeof(FlagAttribute)).First()).Description}\n";
                    }
                }
            }

            return desc;
        }
    }

    public static class MoveList
    {
        public static List<Move> AllMoves { get; set; }
    }
}
