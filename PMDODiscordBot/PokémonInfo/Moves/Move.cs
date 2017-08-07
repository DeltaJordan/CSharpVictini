using System;
using System.Collections.Generic;
using System.Linq;
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

        [JsonProperty("num")]
        public int ItemNum { get; set; }
        [JsonProperty("accuracy")]
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

//        public override string ToString()
//        {
//            Attribute.GetCustomAttributes(this, typeof(FlagsAttribute))
//        }
    }
}
