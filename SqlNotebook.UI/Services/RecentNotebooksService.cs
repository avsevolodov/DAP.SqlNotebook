using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace DAP.SqlNotebook.UI.Services;

/// <summary>Client-side recent notebooks (localStorage).</summary>
public interface IRecentNotebooksService
{
    Task<IReadOnlyList<RecentNotebookEntry>> GetRecentAsync(int maxCount = 10);
    Task AddRecentAsync(Guid id, string name);
}

public sealed record RecentNotebookEntry(Guid Id, string Name);

public sealed class RecentNotebooksService : IRecentNotebooksService
{
    private const string StorageKey = "sqlnotebook:recent";
    private const int DefaultMaxCount = 10;
    private readonly IJSRuntime _js;

    public RecentNotebooksService(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    public async Task<IReadOnlyList<RecentNotebookEntry>> GetRecentAsync(int maxCount = DefaultMaxCount)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<RecentNotebookEntry>();
            var list = JsonSerializer.Deserialize<List<RecentNotebookEntryDto>>(json);
            if (list == null || list.Count == 0)
                return Array.Empty<RecentNotebookEntry>();
            return list
                .Where(x => x.Id != default && !string.IsNullOrEmpty(x.Name))
                .Take(maxCount)
                .Select(x => new RecentNotebookEntry(x.Id, x.Name!))
                .ToList();
        }
        catch
        {
            return Array.Empty<RecentNotebookEntry>();
        }
    }

    public async Task AddRecentAsync(Guid id, string name)
    {
        if (id == default || string.IsNullOrWhiteSpace(name))
            return;
        try
        {
            var list = (await GetRecentAsync(DefaultMaxCount + 5)).ToList();
            list.RemoveAll(x => x.Id == id);
            list.Insert(0, new RecentNotebookEntry(id, name.Trim()));
            var toSave = list.Take(DefaultMaxCount)
                .Select(x => new RecentNotebookEntryDto { Id = x.Id, Name = x.Name })
                .ToList();
            var json = JsonSerializer.Serialize(toSave);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
            // ignore
        }
    }

    private sealed class RecentNotebookEntryDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}
