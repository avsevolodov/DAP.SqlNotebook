using System.Text.Json;
using System.Text.Json.Serialization;

namespace DAP.SqlNotebook.Service.Client
{
    public static class JsonHelper
    {
        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _options);
        }

        public static byte[] SerializeToUtf8Bytes<T>(T value)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> data)
        {
            return JsonSerializer.Deserialize<T>(data, _options);
        }

        private static readonly JsonSerializerOptions _options = new()
        {
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null
        };
    }
}
