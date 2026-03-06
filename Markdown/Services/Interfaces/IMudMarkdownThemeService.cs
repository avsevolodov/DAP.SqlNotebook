using System;

namespace DAP.Markdown;

public interface IMudMarkdownThemeService
{
	event EventHandler<CodeBlockTheme> CodeBlockThemeChanged;

	void SetCodeBlockTheme(CodeBlockTheme theme);
}