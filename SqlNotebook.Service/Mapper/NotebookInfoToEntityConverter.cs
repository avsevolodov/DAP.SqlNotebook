using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Mapper;

public sealed class NotebookInfoToEntityConverter : ITypeConverter<NotebookInfo, NotebookEntity>
{
    public NotebookEntity Convert(NotebookInfo source, NotebookEntity destination, ResolutionContext context)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var cells = (source.Cells ?? Array.Empty<NotebookCellInfo>())
            .Select((c, i) => new NotebookCellEntity
            {
                Id = c.Id,
                NotebookId = source.Id,
                OrderIndex = i,
                CellType = (NotebookCellTypeEntity)c.Type,
                Content = c.Content ?? string.Empty,
                ExecutionResultJson = SerializeExecutionResult(c.ExecutionResult),
                CreatedBy = c.CreatedBy,
                Title = c.Title,
                CatalogNodeId = c.CatalogNodeId,
                DatabaseDisplayName = c.DatabaseDisplayName,
            })
            .ToList();

        return new NotebookEntity
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceId = source.WorkspaceId,
            CatalogNodeId = source.CatalogNodeId,
            CatalogNodeDisplayName = source.CatalogNodeDisplayName,
            NotebookType = (int)source.NotebookType,
            Status = (int)source.Status,
            TagsJson = SerializeTags(source.Tags),
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy,
            Cells = cells,
        };
    }

    private static string? SerializeTags(List<string>? tags)
    {
        if (tags == null || tags.Count == 0) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(tags);
        }
        catch { return null; }
    }

    private static string? SerializeExecutionResult(NotebookCellExecutionResultInfo? result)
    {
        if (result == null)
            return null;
        try
        {
            return JsonSerializer.Serialize(result);
        }
        catch
        {
            return null;
        }
    }
}
