using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSharpDewott.Deserialization.Converters
{
    class UnixTimeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateTime? time = ((DateTimeOffset)value).DateTime;

            writer.WriteValue(time);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }
            else
            {
                if (int.TryParse(reader.Value.ToString(), out int result))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(result).DateTime;
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime?);
        }
    }
}
