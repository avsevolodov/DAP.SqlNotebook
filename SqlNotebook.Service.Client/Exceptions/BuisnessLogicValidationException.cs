namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class BusinessLogicValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public BusinessLogicValidationException(IDictionary<string, string[]> errors) : base("Validation exception")
    {
        Errors = errors;
    }

    public BusinessLogicValidationException(string property, string[] errors) : base("Validation exception")
    {
        Errors = new Dictionary<string, string[]>();
        Errors[property] = errors;
    }
}