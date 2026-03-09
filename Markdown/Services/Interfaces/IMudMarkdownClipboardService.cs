using System.Threading.Tasks;

namespace DAP.Markdown;

public interface IMudMarkdownClipboardService
{
	ValueTask CopyToClipboardAsync(string text);
}
