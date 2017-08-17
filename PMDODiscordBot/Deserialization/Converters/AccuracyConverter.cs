using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSharpDewott.Deserialization.Converters
{
    public class AccuracyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            int accuracyValue = (int)value;

            if (accuracyValue >= 0)
            {
                writer.WriteValue(accuracyValue);
            }
            else
            {
                writer.WriteValue("true");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (int.TryParse(reader.Value.ToString(), out int result))
            {
                return result;
            }
            else
            {
                return -1;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool) || objectType == typeof(int);
        }
    }
}
