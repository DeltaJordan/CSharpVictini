using System;

namespace CSharpDewott.PokémonInfo.Moves
{
    public class FlagAttribute : Attribute
    {
        public FlagAttribute(string description)
        {
            this.Description = description;
        }

        public string Description { get; }
    }
}
