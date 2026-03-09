namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class BadRequestHttpClientException<TExceptionData> : HttpClientException<TExceptionData> where TExceptionData : class
{
    public BadRequestHttpClientException(TExceptionData? exceptionData)
        : base(exceptionData)
    {
    }
}