namespace DAP.SqlNotebook.Service.Client.V1;

public interface ISchemaClient
{
    Task<SchemaDto> GetSchemaAsync(Guid? catalogNodeId, CancellationToken ct);
    Task<IReadOnlyList<SchemaAutocompleteItem>> AutocompleteAsync(SchemaAutocompleteRequest request, CancellationToken ct);
}

