namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class NotFoundHttpClientException<TExceptionData> : HttpClientException<TExceptionData> where TExceptionData : class
{
    public NotFoundHttpClientException(TExceptionData? exceptionData)
        : base(exceptionData)
    {
    }
}