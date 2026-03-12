using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Mapper;

public sealed class NotebookEntityToInfoConverter : ITypeConverter<NotebookEntity, NotebookInfo>
{
    public NotebookInfo Convert(NotebookEntity source, NotebookInfo destination, ResolutionContext context)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var cells = (source.Cells ?? Array.Empty<NotebookCellEntity>())
            .OrderBy(c => c.OrderIndex)
            .Select(c => new NotebookCellInfo
            {
                Id = c.Id,
                Type = (int)c.CellType == 2 ? NotebookCellTypeInfo.Sql : (NotebookCellTypeInfo)c.CellType,
                Content = c.Content ?? string.Empty,
                ExecutionResult = ParseExecutionResult(c.ExecutionResultJson),
                CreatedBy = c.CreatedBy,
                Title = c.Title,
                CatalogNodeId = c.CatalogNodeId,
                DatabaseDisplayName = c.DatabaseDisplayName,
            })
            .ToList();

        return new NotebookInfo
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceId = source.WorkspaceId,
            CatalogNodeId = source.CatalogNodeId,
            CatalogNodeDisplayName = source.CatalogNodeDisplayName,
            NotebookType = (NotebookTypeInfo)source.NotebookType,
            Status = (NotebookStatusInfo)source.Status,
            Tags = ParseTags(source.TagsJson),
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy,
            Cells = cells,
        };
    }

    private static List<string>? ParseTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return null;
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(tagsJson);
            return list?.Count > 0 ? list : null;
        }
        catch { return null; }
    }

    private static NotebookCellExecutionResultInfo ParseExecutionResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new NotebookCellExecutionResultInfo();
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var result = JsonSerializer.Deserialize<NotebookCellExecutionResultInfo>(json, options);
            return result ?? new NotebookCellExecutionResultInfo();
        }
        catch
        {
            return new NotebookCellExecutionResultInfo();
        }
    }
}
