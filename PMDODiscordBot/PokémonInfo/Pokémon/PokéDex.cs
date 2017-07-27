using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpDewott.PokémonInfo.Pokémon
{
    public class PokéDex
    {
        public readonly List<Pokémon> AllPokémon;

        public static PokéDex InstanceDex
        {
            get;
            private set;
        }

        public PokéDex()
        {
            PokémonInitialization.Initialize();

            this.AllPokémon = new List<Pokémon>();
            this.AllPokémon.AddRange(PokémonInitialization.preInitPokemonList);

            for (int i = 0; i < PokémonInitialization.preInitPokemonList.Count; i++)
            {
                Pokémon pokémon = PokémonInitialization.preInitPokemonList[i];

                if (pokémon.EvolvesFrom != null)
                {
                    for (int j = 0; j < pokémon.EvolvesFrom.Count; j++)
                    {
                        Pokémon pokémonevofrom = pokémon.EvolvesFrom[j];
                        this.AllPokémon[i].EvolvesFrom[j] = this.AllPokémon.FirstOrDefault(e => string.Equals(e.SpeciesName, pokémonevofrom.SpeciesName, StringComparison.CurrentCultureIgnoreCase));
                    }
                }

                if (pokémon.EvolvesInto != null)
                {
                    for (int j = 0; j < pokémon.EvolvesInto.Count; j++)
                    {
                        Pokémon pokémonevofrom = pokémon.EvolvesInto[j];
                        this.AllPokémon[i].EvolvesInto[j] = this.AllPokémon.FirstOrDefault(e => string.Equals(e.SpeciesName, pokémonevofrom.SpeciesName, StringComparison.CurrentCultureIgnoreCase));
                    }
                }
            }

            PokémonInitialization.preInitPokemonList.Clear();

            InstanceDex = this;
        }
    }
}
