namespace DAP.SqlNotebook.Service.Client.Exceptions;

public class InternalServerErrorHttpClientException<TExceptionData> : HttpClientException<TExceptionData> where TExceptionData : class
{
    public InternalServerErrorHttpClientException(TExceptionData? exceptionData)
        : base(exceptionData)
    {
    }
}