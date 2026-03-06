using System;
using Markdig.Syntax.Inlines;
using MudBlazor;

namespace DAP.Markdown;

/// <summary>
/// Provides behaviour properties for Markdown elements.
/// </summary>
public sealed class MudMarkdownProps
{
	/// <summary>
	/// Behaviour properties for the link.
	/// </summary>
	public LinkProps Link { get; } = new();

	/// <summary>
	/// Behaviour properties for the heading.
	/// </summary>
	public HeadingProps Heading { get; } = new();

	/// <summary>
	/// Behaviour properties for code blocks.
	/// </summary>
	public CodeBlockProps CodeBlock { get; } = new();

	/// <summary>
	/// Behaviour properties for the link.
	/// </summary>
	public sealed class LinkProps
	{
		/// <summary>
		/// Command which is invoked when a link is clicked.<br/>
		/// If <c>null</c> a link is opened in the browser.
		/// </summary>
		public ICommand? Command { get; set; }

		/// <summary>
		/// Override the original URL address of the <see cref="LinkInline"/>.<br/>
		/// If a function is not provided <see cref="LinkInline.Url"/> is used.
		/// </summary>
		public Func<LinkInline, string?>? OverrideUrl { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the <c>target="_blank"</c> attribute is disabled for <b>external</b> links.
		/// </summary>
		public bool DisableTargetBlank { get; set; }
	}

	/// <summary>
	/// Behaviour properties for the heading.
	/// </summary>
	public sealed class HeadingProps
	{
		/// <summary>
		/// Typography variant to use for Heading Level 1-6.<br/>
		/// If a function is not provided a default typo for each level is set (e.g. for &lt;h1&gt; it will be <see cref="Typo.h1"/>, etc.).
		/// </summary>
		public Func<Typo, Typo>? OverrideTypo { get; set; }
	}

	/// <summary>
	/// Behaviour properties for code blocks.
	/// </summary>
	public sealed class CodeBlockProps
	{
		/// <summary>
		/// Optional custom buttons for code blocks. Receives (code, language) and returns a RenderFragment (e.g. Run button for sql).
		/// If null, no custom buttons are rendered.
		/// </summary>
		public Func<string, string, RenderFragment>? CustomButtons { get; set; }
	}
}
