namespace DAP.SqlNotebook.Service.Client.Exceptions
{
    public static class HttpClientExtensions
    {
        public static async Task<TResponse> CdpReadContentAsAsync<TResponse>(
            this HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            var responseJson = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            var response = JsonHelper.Deserialize<TResponse>(responseJson);

            if (response == null)
            {
                throw new HttpClientQueryException(
                    $"Http request GET {responseMessage.RequestMessage?.RequestUri?.AbsolutePath} gets null response from server.");
            }

            return response;
        }
    }
}
