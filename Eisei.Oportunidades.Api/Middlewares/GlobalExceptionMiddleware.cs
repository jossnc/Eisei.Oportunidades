using System.ComponentModel.DataAnnotations;
using Eisei.Oportunidades.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Eisei.Oportunidades.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            RequestValidationException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        if (exception is RequestValidationException validationException)
        {
            var validationProblem = new ValidationProblemDetails(validationException.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "Se han encontrado errores de validación.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            await context.Response.WriteAsJsonAsync(validationProblem);
            return;
        }

        if (exception is ValidationException validationExceptionBase)
        {
            var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["error"] = [validationExceptionBase.Message]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = validationExceptionBase.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            await context.Response.WriteAsJsonAsync(validationProblem);
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad request",
                StatusCodes.Status404NotFound => "Not found",
                StatusCodes.Status409Conflict => "Conflict",
                _ => "Internal server error"
            },
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "Ocurrió un error inesperado. Por favor, inténtelo más tarde."
                : exception.Message,
            Type = "https://tools.ietf.org/html/rfc7231"
        };

        _logger.LogError(exception, "Unhandled exception in API.");
        await context.Response.WriteAsJsonAsync(problem);
    }
}
