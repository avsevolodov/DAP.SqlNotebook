namespace DAP.SqlNotebook.UI.Helpers;

using System.Collections.Generic;

/// <summary>
/// Represents a segment of parsed markdown content: either plain text or a code block.
/// </summary>
public sealed class MarkdownSegment
{
    public bool IsCodeBlock { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;

    public static List<MarkdownSegment> Parse(string? content)
    {
        var result = new List<MarkdownSegment>();
        if (string.IsNullOrEmpty(content))
            return result;

        var regex = new System.Text.RegularExpressions.Regex(
            @"```([^\s\r\n]*)\s*\r?\n([\s\S]*?)```",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        var lastIndex = 0;
        foreach (System.Text.RegularExpressions.Match m in regex.Matches(content))
        {
            var textBefore = content[lastIndex..m.Index];
            if (!string.IsNullOrEmpty(textBefore))
                result.Add(new MarkdownSegment { IsCodeBlock = false, Content = textBefore });

            var lang = (m.Groups[1].Value ?? "").Trim();
            var code = (m.Groups[2].Value ?? "").TrimEnd();
            result.Add(new MarkdownSegment
            {
                IsCodeBlock = true,
                Language = lang,
                Content = code
            });
            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < content.Length)
        {
            var textAfter = content[lastIndex..];
            if (!string.IsNullOrEmpty(textAfter))
                result.Add(new MarkdownSegment { IsCodeBlock = false, Content = textAfter });
        }

        return result;
    }
}
