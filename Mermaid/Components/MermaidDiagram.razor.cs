using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace SqlNotebook.Mermaid.Components;

/// <summary>
/// Blazor component that renders Mermaid diagram from source text.
/// Requires the host to include script from _content/Mermaid/mermaid-diagram.js (loads mermaid.js and defines run).
/// </summary>
public class MermaidDiagram : ComponentBase
{
	private readonly string _elementId = "mermaid-" + Guid.NewGuid().ToString("N")[..8];

	[Parameter]
	public string Text { get; set; } = string.Empty;

	[Parameter]
	public string? CssClass { get; set; }

	[Inject]
	private IJSRuntime Js { get; init; } = null!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var idx = 0;
		var cssClass = "mermaid-diagram-container " + (CssClass ?? "");
		builder.OpenElement(idx++, "div");
		builder.AddAttribute(idx++, "class", cssClass.Trim());
		builder.AddAttribute(idx++, "id", _elementId);
		builder.AddContent(idx++, Text);
		builder.CloseElement();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (string.IsNullOrWhiteSpace(Text))
			return;
		try
		{
			await Js.InvokeVoidAsync("MermaidDiagram.run", _elementId).ConfigureAwait(false);
		}
		catch
		{
			// mermaid.js may not be loaded or diagram invalid
		}
	}
}
