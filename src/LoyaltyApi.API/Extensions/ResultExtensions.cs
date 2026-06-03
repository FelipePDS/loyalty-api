using LoyaltyApi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LoyaltyApi.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return MapError(result.Error!);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return MapError(result.Error!);
    }

    public static IResult ToCreatedHttpResult<T>(this Result<T> result, string? location = null)
    {
        if (result.IsSuccess)
            return location is not null
                ? Results.Created(location, result.Value)
                : Results.Created((string?)null, result.Value);

        return MapError(result.Error!);
    }

    private static IResult MapError(Error error)
    {
        var problemDetails = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = GetStatusCode(error.Type)
        };

        if (error is ValidationError validationError)
        {
            return Results.UnprocessableEntity(new ValidationProblemDetails(
                validationError.Errors.ToDictionary(k => k.Key, v => v.Value))
            {
                Title = error.Code,
                Detail = error.Message,
                Status = 422
            });
        }

        return Results.Problem(problemDetails);
    }

    private static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => 422,
        ErrorType.NotFound => 404,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.Conflict => 409,
        ErrorType.Internal => 500,
        _ => 400
    };
}
