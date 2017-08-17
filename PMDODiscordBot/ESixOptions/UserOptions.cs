using System.Collections.Generic;

namespace CSharpDewott.ESixOptions
{
    class UserOptions
    {
        public ulong Id { get; set; }
        public bool DisplaySources { get; set; }
        public bool DisplayTags { get; set; }
        public bool DisplayId { get; set; }
        public List<string> BlackList { get; set; }
    }
}
