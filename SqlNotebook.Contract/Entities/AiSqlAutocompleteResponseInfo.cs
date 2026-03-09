namespace DAP.SqlNotebook.Contract.Entities;

public class AiSqlAutocompleteResponseInfo
{
    public string Suggestion { get; set; } = string.Empty;
    public List<AiSqlSuggestionItemInfo>? Suggestions { get; set; }
}
