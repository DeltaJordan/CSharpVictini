using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSharpDewott.PokémonInfo.Moves
{
    public static class MoveHelper
    {
        public static void InitializeMoves()
        {
            try
            {

                MoveList.AllMoves = JsonConvert.DeserializeObject<List<Move>>(File.ReadAllText(Path.Combine(Program.AppPath, "Data", "moves.json")), new JsonSerializerSettings
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
