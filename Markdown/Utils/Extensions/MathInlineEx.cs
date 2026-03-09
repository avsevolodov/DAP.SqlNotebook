using Markdig.Extensions.Mathematics;

namespace DAP.Markdown;

internal static class MathInlineEx
{
	public static string GetDelimiter(this MathInline @this) =>
		string.Create(@this.DelimiterCount, @this.Delimiter, static (span, c) =>
		{
			for (var i = 0; i < span.Length; i++)
				span[i] = c;
		});
}
