using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DAP.SqlNotebook.Service.Middleware;

internal class ExceptionHandlingMiddleware : IExceptionHandler
{
    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> log, ProblemDetailsFactory detailsFactory)
    {
        _log = log;
        _detailsFactory = detailsFactory;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken cancellationToken)
    {
        var errorResult = GetErrorResult(ex);
        var statusCode = (int)errorResult.HttpStatusCode;

        _log.Log(errorResult.LogLevel, ex, "Error executing request.");

        object problem;

        if (statusCode < (int)HttpStatusCode.InternalServerError)
        {
            problem = _detailsFactory.CreateProblemDetails(
                context,
                title: "An error occurred while processing request.",
                statusCode: statusCode,
                type: "???",
                detail: ex.Message);
        }
        else
        {
            problem = _detailsFactory.CreateProblemDetails(
                context,
                title: "Internal server error occurred while processing request.",
                statusCode: statusCode,
                type: "https://tools.ietf.org/html/rfc9110#name-server-error-5xx",
                detail: ex.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private ErrorResult GetErrorResult(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => new ErrorResult(HttpStatusCode.Forbidden, LogLevel.Warning),
            // NotFoundException => new ErrorResult(HttpStatusCode.NotFound, LogLevel.Warning),
            ArgumentException => new ErrorResult(HttpStatusCode.BadRequest, LogLevel.Warning),
            _ => new ErrorResult(HttpStatusCode.InternalServerError, LogLevel.Error)
        };
    }

    private readonly ILogger<ExceptionHandlingMiddleware> _log;
    private readonly ProblemDetailsFactory _detailsFactory;

    private readonly struct ErrorResult(HttpStatusCode httpStatusCode, LogLevel logLevel)
    {
        public HttpStatusCode HttpStatusCode { get; } = httpStatusCode;
        public LogLevel LogLevel { get; } = logLevel;
    }
}