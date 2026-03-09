using System.Threading;
using System.Threading.Tasks;

namespace DAP.Markdown;

internal interface IMudMarkdownValueProvider
{
    ValueTask<string> GetValueAsync(string value, MarkdownSourceType sourceType, CancellationToken ct = default);
}
