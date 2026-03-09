namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class ProblemDetailsData
{
    public ProblemDetailsData(string detail)
    {
        Detail = detail;
    }

    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public string GetErrors()
    {
        var messages = new List<string>();

        if (!string.IsNullOrWhiteSpace(Detail))
        {
            messages.Add(Detail);
        }

        if (Errors == null)
        {
            return string.Join(", ", messages);
        }

        messages.AddRange(Errors.SelectMany(entry => entry.Value));

        return string.Join(", ", messages);
    }

    public override string ToString()
    {
        if (Detail != null)
        {
            return Detail;
        }

        return Title ?? "Internal server error.";
    }
}