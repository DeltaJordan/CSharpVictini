using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpDewott.Deserialization.Converters;
using Newtonsoft.Json;

namespace CSharpDewott.Deserialization
{
    public class ESixImage
    {
        public enum E621Rating
        {
            Safe,
            Questionable,
            Explict
        }

        [JsonProperty("id")]
        public ulong Id { get; set; }
        /*[JsonProperty("children")]
        public ulong ChildId { get; set; }
        [JsonProperty("parent_id")]
        public ulong ParentId { get; set; }*/
        [JsonProperty("tags"), JsonConverter(typeof(StringListConverter))]
        public List<string> Tags { get; set; }
        [JsonProperty("locked_tags"), JsonConverter(typeof(StringListConverter))]
        public List<string> LockedTags { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
//      [JsonProperty("created_at"), JsonConverter(typeof(UnixTimeConverter))]
//      public DateTime CreatedAt { get; set; }
        [JsonProperty("creator_id")]
        public ulong CreatorId { get; set; }
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("source")]
        public string Source { get; set; }
        [JsonProperty("score")]
        public int Score { get; set; }
        [JsonProperty("fav_count")]
        public int FavoriteCount { get; set; }
        [JsonProperty("md5")]
        public string MD5 { get; set; }
        [JsonProperty("preview_url")]
        public string IconUrl { get; set; }
        [JsonProperty("preview_width")]
        public int? IconWidth { get; set; }
        [JsonProperty("preview_height")]
        public int? IconHeight { get; set; }
        [JsonProperty("file_url")]
        public string ImageUrl { get; set; }
        [JsonProperty("width")]
        public int? ImageWidth { get; set; }
        [JsonProperty("height")]
        public int? ImageHeight { get; set; }
        [JsonProperty("file_size")]
        public ulong FileSize { get; set; }
        [JsonProperty("file_ext")]
        public string FileExtension { get; set; }
        [JsonProperty("rating"), JsonConverter(typeof(E621RatingConverter))]
        public E621Rating Rating { get; set; }
        [JsonProperty("has_comments")]
        public bool HasComments { get; set; }
        [JsonProperty("has_children")]
        public bool HasChildren { get; set; }
        [JsonProperty("has_notes")]
        public bool HasNotes { get; set; }
        [JsonProperty("artist")]
        public string[] Artists { get; set; }
        [JsonProperty("sources")]
        public string[] Sources { get; set; }
    }

    internal class E621RatingConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ESixImage.E621Rating rating = (ESixImage.E621Rating)value;

            switch (rating)
            {
                case ESixImage.E621Rating.Safe:
                {
                    writer.WriteValue("s");
                }
                    break;
                case ESixImage.E621Rating.Questionable:
                {
                    writer.WriteValue("q");
                }
                    break;
                case ESixImage.E621Rating.Explict:
                {
                    writer.WriteValue("e");
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.Value.ToString())
            {
                case "s":
                case "safe":
                    return ESixImage.E621Rating.Safe;
                case "q":
                case "questionable":
                    return ESixImage.E621Rating.Questionable;
                case "e":
                case "explict":
                    return ESixImage.E621Rating.Explict;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ESixImage.E621Rating);
        }
    }
}
