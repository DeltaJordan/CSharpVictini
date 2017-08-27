using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSharpDewott.Deserialization.Converters
{
    class StringListConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            List<string> stringList = (List<string>)value;

            writer.WriteValue(value == null ? "null" : string.Join(" ", stringList));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value?.ToString().Split(' ').ToList();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>);
        }
    }
}
