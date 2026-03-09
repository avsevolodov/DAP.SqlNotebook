using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DAP.SqlNotebook.Service.Services;

/// <summary>
/// JSON options for AI SQL Python API: request/response use snake_case.
/// </summary>
internal static class SnakeCaseJsonOptions
{
    /// <summary>
    /// Naming policy: snake_case &lt;-&gt; PascalCase (e.g. chart_type &lt;-&gt; ChartType).
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = new SnakeCaseToPascalCaseNamingPolicy(),
        PropertyNameCaseInsensitive = true
    };

    private sealed class SnakeCaseToPascalCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new StringBuilder();
            var nextUpper = true;
            foreach (var c in name)
            {
                if (c == '_')
                {
                    nextUpper = true;
                    continue;
                }
                sb.Append(nextUpper ? char.ToUpper(c, CultureInfo.InvariantCulture) : char.ToLower(c, CultureInfo.InvariantCulture));
                nextUpper = false;
            }
            return sb.ToString();
        }
    }
}
