namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class HttpClientQueryException : Exception
{
    public HttpClientQueryException() { }
    public HttpClientQueryException(string message) : base(message) { }
    public HttpClientQueryException(string message, Exception inner) : base(message, inner) { }
}