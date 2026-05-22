using ATLAS.Kernel.Domain.Result;
using ATLAS.Kernel.i18n.API.Controllers;
using ATLAS.Kernel.i18n.Application.Contracts.Requests;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;
using ATLAS.Kernel.i18n.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Controllers;

public sealed class TranslationsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly TranslationsController _controller;
    public TranslationsControllerTests() => _controller = new TranslationsController(_mediator.Object);

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<CreateTranslationCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<TranslationDto>.Ok(T())); Assert.IsType<CreatedAtActionResult>(await _controller.Create(new CreateTranslationCommand("CRM0001","es-ES","txt"),default));
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<UpdateTranslationCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<TranslationDto>.Ok(T())); Assert.IsType<OkObjectResult>(await _controller.Update("CRM0001","es-ES",new UpdateTranslationRequest("x","c"),default));
    }

    [Fact]
    public async Task SetReviewed_ReturnsNoContent()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<SetTranslationReviewedCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok()); Assert.IsType<NoContentResult>(await _controller.SetReviewed("CRM0001","es-ES",new SetReviewedRequest(true),default));
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<DeleteTranslationCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok()); Assert.IsType<NoContentResult>(await _controller.Delete("CRM0001","es-ES",default));
    }

    [Fact]
    public async Task BulkUpsert_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<BulkUpsertTranslationsCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<BulkUpsertResultDto>.Ok(new BulkUpsertResultDto(1,0,[]))); Assert.IsType<OkObjectResult>(await _controller.BulkUpsert(new BulkUpsertTranslationsCommand([]),default));
    }

    [Fact]
    public async Task GetTranslation_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<GetTranslationQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<TranslationLookupDto>.Ok(new TranslationLookupDto("CRM0001","es-ES","txt",false,"es-ES"))); Assert.IsType<OkObjectResult>(await _controller.GetTranslation("CRM0001","es-ES",default));
    }

    [Fact]
    public async Task GetModuleTranslations_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<GetTranslationsByModuleQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<TranslationMapDto>.Ok(new TranslationMapDto("es-ES","CRM",new Dictionary<string,string>()))); Assert.IsType<OkObjectResult>(await _controller.GetModuleTranslations("CRM","es-ES",default));
    }

    [Fact]
    public async Task GetPaged_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<GetTranslationsPagedQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<ATLAS.Kernel.Infrastructure.Pagination.PagedResult<TranslationDto>>.Ok(null!)); Assert.IsType<OkObjectResult>(await _controller.GetPaged(1,10,null,null,null,default));
    }

    [Fact]
    public async Task GetVariants_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<GetTranslationVariantsQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyList<TranslationDto>>.Ok(new List<TranslationDto>{T()})); Assert.IsType<OkObjectResult>(await _controller.GetVariants("CRM0001",default));
    }

    private static TranslationDto T() =>
        new (
            Guid.NewGuid(),
            "CRM0001",
            "CRM",
            "es-ES",
            "txt",
            null,
            null,
            false,
            DateTimeOffset.UtcNow,
            "t",
            null,
            null);
}
