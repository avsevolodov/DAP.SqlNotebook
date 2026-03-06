using System.Net;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client
{
    public abstract class HttpResponseMessageHelper<TExceptionData> where TExceptionData : class
    {
        public static async Task EnsureSuccessStatusCode(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                return;
            }

            var responseJson = await responseMessage.Content.ReadAsStringAsync();

            var exceptionData = TryParseExceptionData(responseJson);

            if (ExceptionActions.TryGetValue(responseMessage.StatusCode, out var exceptionAction))
            {
                exceptionAction(exceptionData);
            }

            throw new HttpClientException<TExceptionData>(exceptionData);
        }

        public static Dictionary<HttpStatusCode, Action<TExceptionData?>> ExceptionActions { get; set; } = null!;

        private static TExceptionData? TryParseExceptionData(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                return null;
            }

            try
            {
                return JsonHelper.Deserialize<TExceptionData>(responseJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
