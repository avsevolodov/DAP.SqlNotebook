using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.UI.Helpers;

/// <summary>Deserializes ConnectionStatusInfo from either integer (0,1,2) or string ("Unknown","Ok","Failed").</summary>
public sealed class ConnectionStatusInfoConverter : JsonConverter<ConnectionStatusInfo>
{
    public override ConnectionStatusInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var n))
                {
                    if (n == 0) return ConnectionStatusInfo.Unknown;
                    if (n == 1) return ConnectionStatusInfo.Ok;
                    if (n == 2) return ConnectionStatusInfo.Failed;
                    return (ConnectionStatusInfo)n;
                }
                break;
            case JsonTokenType.String:
                var s = reader.GetString()?.Trim();
                if (string.IsNullOrEmpty(s)) return ConnectionStatusInfo.Unknown;
                if (string.Equals(s, "Ok", StringComparison.OrdinalIgnoreCase)) return ConnectionStatusInfo.Ok;
                if (string.Equals(s, "Failed", StringComparison.OrdinalIgnoreCase)) return ConnectionStatusInfo.Failed;
                if (string.Equals(s, "Unknown", StringComparison.OrdinalIgnoreCase)) return ConnectionStatusInfo.Unknown;
                break;
        }

        return ConnectionStatusInfo.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, ConnectionStatusInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
