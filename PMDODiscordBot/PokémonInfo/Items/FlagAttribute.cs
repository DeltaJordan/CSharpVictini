using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpDewott.PokémonInfo.Items
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
