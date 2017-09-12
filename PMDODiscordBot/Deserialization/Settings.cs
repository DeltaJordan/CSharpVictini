using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CSharpDewott.Deserialization
{
    class Settings
    {
        public string Token { get; set; }
        public string DictKey { get; set; }
        public string ThesKey { get; set; }
        public string ImgurClientId { get; set; }
        public string RedditUsername { get; set; }
        public string RedditPass { get; set; }
        public string RedditClientID { get; set; }
        public string RedditSecret { get; set; }
        public string CleverBotUser { get; set; }
        public string CleverBotKey { get; set; }
        public string YoutubeApiKey { get; set; }
    }
}
