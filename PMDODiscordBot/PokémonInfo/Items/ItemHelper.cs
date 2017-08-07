using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSharpDewott.PokémonInfo.Items
{
    public static class ItemHelper
    {
        public static void InitializeItems()
        {
            try
            {

                ItemList.AllItems = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(Path.Combine(Program.AppPath, "Data", "items.json")), new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
