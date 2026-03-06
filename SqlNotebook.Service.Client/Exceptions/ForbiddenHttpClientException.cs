namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class ForbiddenHttpClientException<TExceptionData> : HttpClientException<TExceptionData> where TExceptionData : class
{
    public ForbiddenHttpClientException(TExceptionData? exceptionData)
        : base(exceptionData)
    {
    }
}