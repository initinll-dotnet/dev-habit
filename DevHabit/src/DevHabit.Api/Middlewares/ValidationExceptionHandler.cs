using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Middlewares;

public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) 
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var errors = validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                x => x.Key.ToLowerInvariant(),
                x => x.Select(y => y.ErrorMessage).ToArray());

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Detail = "one or more validation errors occured",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = errors },
            },
        };

        return await problemDetailsService.TryWriteAsync(context);
    }
}
