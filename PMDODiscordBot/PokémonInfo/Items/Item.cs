using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSharpDewott.PokémonInfo.Items
{
    public class Item
    {
        private int genNum;

        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("spritenum")]
        public int SpriteNum { get; set; }
        [JsonProperty("fling")]
        public FlingInfo Fling { get; set; }
        [JsonProperty("megaStone")]
        public string MegaStone { get; set; }
        [JsonProperty("megaEvolves")]
        public string MegaEvolves { get; set; }
        [JsonProperty("num")]
        public int ItemNum { get; set; }
        [JsonProperty("gen")]
        public int GenNum
        {
            get => this.genNum;
            set
            {
                this.Generation = new ItemGeneration(value);
                this.genNum = value;
            }
        }
        public ItemGeneration Generation { get; set; }
        [JsonProperty("desc")]
        public string Description { get; set; }
        [JsonProperty("isBerry")]
        public bool IsBerry { get; set; }
        [JsonProperty("naturalGift")]
        public NaturalGift NaturalGiftInfo { get; set; }



        public class ItemGeneration
        {
            private int genNumber;

            public ItemGeneration(int genNum)
            {
                this.genNumber = genNum;
            }

            public string GetName()
            {
                switch (this.genNumber)
                {
                    case 1:
                        return "Gen I";
                    case 2:
                        return "Gen II";
                    case 3:
                        return "Gen III";
                    case 4:
                        return "Gen IV";
                    case 5:
                        return "Gen V";
                    case 6:
                        return "Gen VI";
                    case 7:
                        return "Gen VII";
                    default:
                        return "ERROR";
                }
            }
        }

        public class FlingInfo
        {
            [JsonProperty("basePower")]
            public int BasePower { get; set; }
        }

        public class NaturalGift
        {
            [JsonProperty("basePower")]
            public int BasePower { get; set; }
            [JsonProperty("type")]
            public string MoveType { get; set; }
        }
    }

    public static class ItemList
    {
        public static List<Item> AllItems { get; set; }
    }
}
