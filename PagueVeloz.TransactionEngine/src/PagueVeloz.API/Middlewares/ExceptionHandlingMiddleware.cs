using FluentValidation;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Infrastructure.Observability;
using System.Net;
using System.Text.Json;

namespace PagueVeloz.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception, correlationIdProvider);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, ICorrelationIdProvider correlationIdProvider)
    {
        if (exception is ValidationException validationException)
        {
            await HandleValidationExceptionAsync(context, validationException, correlationIdProvider);
            return;
        }

        var correlationId = GetCorrelationId(context, correlationIdProvider);

        var (statusCode, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ConcurrencyConflictException => (HttpStatusCode.Conflict, "Concurrency conflict"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "{ExceptionType} while processing {Method} {Path}. CorrelationId: {CorrelationId}. Message: {Message}",
                exception.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                correlationId,
                exception.Message);
        }

        var problemDetails = new
        {
            title,
            status = (int)statusCode,
            detail = exception.Message,
            trace_id = correlationId
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }

    private async Task HandleValidationExceptionAsync(
        HttpContext context,
        ValidationException exception,
        ICorrelationIdProvider correlationIdProvider)
    {
        var correlationId = GetCorrelationId(context, correlationIdProvider);

        var errors = string.Join("; ",
            exception.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

        _logger.LogWarning(
            exception,
            "Validation failed while processing {Method} {Path}. CorrelationId: {CorrelationId}. Errors: {Errors}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            errors);

        var problemDetails = new
        {
            title = "Validation failed",
            status = (int)HttpStatusCode.BadRequest,
            errors = exception.Errors
                .GroupBy(e => ToSnakeCase(e.PropertyName))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()),
            trace_id = correlationId
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }

    private static string ToSnakeCase(string value) =>
        string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c)
                ? "_" + c
                : c.ToString()))
        .ToLowerInvariant();

    private static string GetCorrelationId(HttpContext context, ICorrelationIdProvider correlationIdProvider) =>
        !string.IsNullOrWhiteSpace(correlationIdProvider.CorrelationId)
            ? correlationIdProvider.CorrelationId
            : context.TraceIdentifier;
}
