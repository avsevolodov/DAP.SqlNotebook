using System;
using System.Collections.Generic;
using System.Text.Json;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Mapper;

public sealed class NotebookEntityToMetaInfoConverter : ITypeConverter<NotebookEntity, NotebookMetaInfo>
{
    public NotebookMetaInfo Convert(NotebookEntity source, NotebookMetaInfo destination, ResolutionContext context)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new NotebookMetaInfo
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceId = source.WorkspaceId,
            NotebookType = (NotebookTypeInfo)source.NotebookType,
            Status = (NotebookStatusInfo)source.Status,
            Tags = ParseTags(source.TagsJson),
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy,
        };
    }

    private static List<string>? ParseTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return null;
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(tagsJson);
            return list?.Count > 0 ? list : null;
        }
        catch { return null; }
    }
}
