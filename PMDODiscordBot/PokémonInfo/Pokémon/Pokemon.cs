using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Humanizer;
using CSharpDewott.Extensions;
using Discord;
using Color = System.Drawing.Color;

namespace CSharpDewott.PokémonInfo.Pokémon
{
    public class Pokémon
    {
        public enum GenderType
        {
            Ambiguous,
            Male,
            Female,
            Genderless
        }

        public int? DexNum { get; set; }
        public string SpeciesName { get; set; }
        public string BaseSpecies { get; set; }
        public string Form { get; set; }
        public string[] Types { get; set; }
        public GenderRatio GenderRatio { get; set; }
        public BaseStats BaseStats { get; set; }
        public Abilities Abilities { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public Color Color { get; set; }
        public List<Pokémon> EvolvesInto { get; set; }
        public int? EvolvedLevel { get; set; }
        public List<Pokémon> EvolvesFrom { get; set; }
        public List<string> OtherForms { get; set; }
        public EggGroups EggGroups { get; set; }
        public GenderType Gender = GenderType.Ambiguous;

        public static Pokémon PostInitPokémon(string speciesName)
        {
            Pokémon mon = new Pokémon
            {
                SpeciesName = speciesName.Transform(To.TitleCase)
            };

            if (mon.SpeciesName.ToLower() == "mrmime")
            {
                mon.SpeciesName = "Mr. Mime";
            }

            if (mon.SpeciesName.ToLower() == "mimejr")
            {
                mon.SpeciesName = "Mime Jr.";
            }

            if (mon.SpeciesName.ToLower() == "typenull")
            {
                mon.SpeciesName = "Type: Null";
            }

            if (mon.SpeciesName.ToLower() == "hooh")
            {
                mon.SpeciesName = "Ho-Oh";
            }

            if (mon.SpeciesName.ToLower() == "hakamoo")
            {
                mon.SpeciesName = "Hakamo-o";
            }

            if (mon.SpeciesName.ToLower() == "jangmoo")
            {
                mon.SpeciesName = "Jangmo-o";
            }

            if (mon.SpeciesName.ToLower() == "kommoo")
            {
                mon.SpeciesName = "Kommo-o";
            }

            if (mon.SpeciesName.ToLower() == "porygonz")
            {
                mon.SpeciesName = "Porygon-Z";
            }

            if (mon.SpeciesName.Contains('-', '.', ':'))
            {
                //IUserMessage _ = Program.Client.GetUser(228019100008316948).GetOrCreateDMChannelAsync().Result.SendMessageAsync($"Pokémon {mon.SpeciesName} not parsed!").Result;
            }

            return mon;
        }

        /// <summary>
        /// Returns the name of the species.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.SpeciesName;
        }
    }

    public class GenderRatio
    {
        private double[] genderRatio = new double[2];

        public GenderRatio(double maleRatio, double femaleRatio)
        {
            this.genderRatio[0] = maleRatio;
            this.genderRatio[1] = femaleRatio;
        }
        
        public double GetMaleRatio()
        {
            return this.genderRatio[0];
        }

        public double GetFemaleRatio()
        {
            return this.genderRatio[1];
        }
    }

    public class BaseStats
    {
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }

        /// <summary>
        /// Combines all stats in standard format seperated by ':'.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.Hp}:{this.Attack}:{this.Defense}:{this.SpecialAttack}:{this.SpecialDefense}:{this.Speed}";
        }
    }

    public class Abilities
    {
        public string Ability1 { get; set; }
        public string Ability2 { get; set; }
        public string AbilityH { get; set; }
    }

    public class EggGroups
    {
        public string EggGroup1 { get; set; }
        public string EggGroup2 { get; set; }
    }
}
