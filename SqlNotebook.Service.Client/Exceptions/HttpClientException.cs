namespace DAP.SqlNotebook.Service.Client.Exceptions
{
    public class HttpClientException<TExceptionData> : Exception where TExceptionData : class
    {
        public TExceptionData? ExceptionData => _exceptionData;

        public HttpClientException(TExceptionData? exceptionData)
            : base(exceptionData?.ToString())
        {
            _exceptionData = exceptionData;
        }

        public HttpClientException(TExceptionData? exceptionData, Exception inner)
            : base(exceptionData?.ToString(), inner)
        {
            _exceptionData = exceptionData;
        }

        protected readonly TExceptionData? _exceptionData;
    }
}
