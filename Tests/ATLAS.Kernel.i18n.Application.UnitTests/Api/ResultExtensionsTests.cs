using ATLAS.Kernel.Domain.Result;
using ATLAS.Kernel.i18n.API.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Api;

public sealed class ResultExtensionsTests
{
    private sealed class TestController : ControllerBase { }
    private readonly TestController _controller = new();

    [Fact] public void ToActionResult_Success_ReturnsOk() => Assert.IsType<OkObjectResult>(Result<int>.Ok(1).ToActionResult(_controller));
    [Fact] public void ToCreatedResult_Success_ReturnsCreated() => Assert.IsType<CreatedAtActionResult>(Result<int>.Ok(1).ToCreatedResult(_controller, "x", new { id = 1 }));
    [Fact] public void ToNoContentResult_Success_ReturnsNoContent() => Assert.IsType<NoContentResult>(Result.Ok().ToNoContentResult(_controller));

    [Fact] public void ToActionResult_Validation_Returns422() => Assert.IsType<UnprocessableEntityObjectResult>(Error.Validation("c", "m").ToResult<int>().ToActionResult(_controller));
    [Fact] public void ToActionResult_NotFound_Returns404() => Assert.IsType<NotFoundObjectResult>(Error.NotFound("c", "m").ToResult<int>().ToActionResult(_controller));
    [Fact] public void ToActionResult_Conflict_Returns409() => Assert.IsType<ConflictObjectResult>(Error.Conflict("c", "m").ToResult<int>().ToActionResult(_controller));
    [Fact] public void ToActionResult_Forbidden_Returns403() => Assert.IsType<ObjectResult>(Error.Forbidden("c", "m").ToResult<int>().ToActionResult(_controller));
    [Fact] public void ToActionResult_Unauthorized_Returns401() => Assert.IsType<UnauthorizedObjectResult>(Error.Unauthorized("c", "m").ToResult<int>().ToActionResult(_controller));
    [Fact] public void ToActionResult_Unexpected_Returns500() => Assert.IsType<ObjectResult>(Error.Unexpected("c", "m").ToResult<int>().ToActionResult(_controller));
}

internal static class ResultTestExtensions
{
    public static Result<T> ToResult<T>(this Error e) => e;
}

