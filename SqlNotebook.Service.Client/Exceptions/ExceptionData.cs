namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class ExceptionData
{
    public ExceptionData(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public override string ToString()
    {
        return Message;
    }
}