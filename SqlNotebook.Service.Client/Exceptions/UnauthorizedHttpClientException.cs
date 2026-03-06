namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class UnauthorizedHttpClientException<TExceptionData> : HttpClientException<TExceptionData> where TExceptionData : class
{
    public UnauthorizedHttpClientException(TExceptionData? exceptionData)
        : base(exceptionData)
    {
    }
}