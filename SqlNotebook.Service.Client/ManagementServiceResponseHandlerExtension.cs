using System.Net;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client
{
    public static class ManagementServiceResponseHandlerExtension
    {
        public static async Task ManagementServiceEnsureSuccessStatusCode(this HttpResponseMessage responseMessage)
        {
            HttpResponseMessageHelper<ProblemDetailsData>.ExceptionActions = _exceptionActions;
            await HttpResponseMessageHelper<ProblemDetailsData>.EnsureSuccessStatusCode(responseMessage);
        }

        private static readonly Dictionary<HttpStatusCode, Action<ProblemDetailsData?>> _exceptionActions = new()
    {
        {
            HttpStatusCode.BadRequest,
            exceptionData => throw new BadRequestHttpClientException<ProblemDetailsData>(exceptionData)
        },
        {
            HttpStatusCode.Forbidden,
            exceptionData => throw new ForbiddenHttpClientException<ProblemDetailsData>(exceptionData)
        },
        {
            HttpStatusCode.InternalServerError,
            exceptionData => throw new InternalServerErrorHttpClientException<ProblemDetailsData>(exceptionData)
        },
        {
            HttpStatusCode.NotFound,
            exceptionData => throw new NotFoundHttpClientException<ProblemDetailsData>(exceptionData)
        },
        {
            HttpStatusCode.Unauthorized,
            exceptionData => throw new UnauthorizedHttpClientException<ProblemDetailsData>(exceptionData)
        }
    };
    }
}
