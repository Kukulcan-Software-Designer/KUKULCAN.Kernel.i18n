using ATLAS.Kernel.Domain.Result;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Kernel.i18n.API.Extensions;

/// <summary>
/// Extension methods that map <see cref="Result"/> / <see cref="Result{T}"/> from
/// <c>Atlas.SharedKernel.Domain</c> to HTTP <see cref="IActionResult"/> responses.
///
/// <para>
/// Error-type mapping follows the <see cref="ErrorType"/> enum from SharedKernel:
/// <list type="table">
///   <item><term>Validation</term>  <description>422 Unprocessable Entity</description></item>
///   <item><term>NotFound</term>    <description>404 Not Found</description></item>
///   <item><term>Conflict</term>    <description>409 Conflict</description></item>
///   <item><term>Forbidden</term>   <description>403 Forbidden</description></item>
///   <item><term>Unauthorized</term><description>401 Unauthorized</description></item>
///   <item><term>Unexpected</term>  <description>500 Internal Server Error</description></item>
/// </list>
/// </para>
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/>.
    /// On success: returns <c>200 OK</c> with the value serialised as JSON.
    /// On failure: returns the appropriate problem details response.
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : result.Error.ToProblemResult(controller);

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a <c>201 Created</c> response on success,
    /// or a problem details response on failure.
    /// </summary>
    public static IActionResult ToCreatedResult<T>(this Result<T> result, ControllerBase controller, string actionName, object routeValues) =>
        result.IsSuccess ? controller.CreatedAtAction(actionName, routeValues, result.Value) : result.Error.ToProblemResult(controller);

    /// <summary>
    /// Converts a void <see cref="Result"/> to a <c>204 No Content</c> on success,
    /// or a problem details response on failure.
    /// </summary>
    public static IActionResult ToNoContentResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : result.Error.ToProblemResult(controller);

    // ── Internal mapper ───────────────────────────────────────────────────────

    private static IActionResult ToProblemResult(this Error error, ControllerBase controller)
    {
        var problem = new ProblemDetails
        {
            Title  = error.Code,
            Detail = error.Message,
            Extensions = { ["errorCode"] = error.Code },
        };

        return error.Type switch
        {
            ErrorType.Validation   => controller.UnprocessableEntity(problem),
            ErrorType.NotFound     => controller.NotFound(problem),
            ErrorType.Conflict     => controller.Conflict(problem),
            ErrorType.Forbidden    => controller.StatusCode(StatusCodes.Status403Forbidden, problem),
            ErrorType.Unauthorized => controller.Unauthorized(problem),
            _                      => controller.StatusCode(StatusCodes.Status500InternalServerError, problem),
        };
    }
}
