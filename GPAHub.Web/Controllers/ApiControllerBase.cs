using System.Security.Claims;
using GPAHub.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? CurrentStudentId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected Guid RequireStudentId() =>
        CurrentStudentId ?? throw new UnauthorizedAccessException();

    protected ActionResult<T> FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);

    protected ActionResult FromResult(Result result) =>
        result.IsSuccess ? Ok(new { success = true }) : FromError(result.Error!);

    protected ActionResult FromError(Error error)
    {
        var problem = new ProblemDetails
        {
            Title = error.Message,
            Status = StatusCodeFor(error.Type)
        };

        problem.Extensions["code"] = error.Code;

        return StatusCode(problem.Status!.Value, problem);
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
