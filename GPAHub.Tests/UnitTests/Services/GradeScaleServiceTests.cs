using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class GradeScaleServiceTests
{
    private readonly Mock<IGradeScaleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly GradeScaleService _service;

    public GradeScaleServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _service = new GradeScaleService(
            _repo.Object,
            _uow.Object,
            mapper,
            new CreateGradeScaleDtoValidator(),
            new UpdateGradeScaleDtoValidator(),
            new SaveGradeDefinitionDtoValidator());
    }

    [Fact]
    public async Task Create_WithValidDto_SavesAndReturnsDto()
    {
        var dto = new CreateGradeScaleDto("My Scale", "desc");

        var result = await _service.CreateAsync(Guid.NewGuid(), dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("My Scale", result.Value.Name);
        _repo.Verify(r => r.AddAsync(It.IsAny<GradeScale>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_FirstScaleForStudent_AutoActivates()
    {
        var studentId = Guid.NewGuid();
        _repo.Setup(r => r.ListByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.CreateAsync(studentId, new CreateGradeScaleDto("First", null));

        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Create_SecondScale_DoesNotAutoActivate()
    {
        var studentId = Guid.NewGuid();
        var existing = new GradeScale("Existing", studentId);
        existing.Activate();
        _repo.Setup(r => r.ListByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var result = await _service.CreateAsync(studentId, new CreateGradeScaleDto("Second", null));

        Assert.False(result.Value.IsActive);
        Assert.True(existing.IsActive);
    }

    [Fact]
    public async Task Create_WithEmptyName_FailsValidation_AndDoesNotSave()
    {
        var result = await _service.CreateAsync(Guid.NewGuid(), new CreateGradeScaleDto("", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        _repo.Verify(r => r.AddAsync(It.IsAny<GradeScale>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_ForAnotherStudentsScale_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdForStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GradeScale?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task AddDefinition_OnMissingScale_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdForStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GradeScale?)null);

        var result = await _service.AddDefinitionAsync(Guid.NewGuid(), Guid.NewGuid(), new SaveGradeDefinitionDto("A", 90, 100, 4m));

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task AddDefinition_ViolatingOverlap_ReturnsConflict_AndDoesNotSave()
    {
        var scale = CreateActiveScaleWithDefinitions(("A", 90, 100));
        _repo.Setup(r => r.GetByIdForStudentAsync(scale.Id, scale.StudentId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scale);

        var result = await _service.AddDefinitionAsync(
            scale.StudentId!.Value, scale.Id, new SaveGradeDefinitionDto("B", 85, 95, 3m));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetActive_ActivatingValidScale_DeactivatesOtherScales()
    {
        var studentId = Guid.NewGuid();
        var target = new GradeScale("Target", studentId);
        target.AddDefinition("A", 90, 100, 4m);
        var otherActive = new GradeScale("Other", studentId);
        otherActive.Activate();
        _repo.Setup(r => r.GetByIdForStudentAsync(target.Id, studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _repo.Setup(r => r.ListByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GradeScale> { target, otherActive });

        var result = await _service.SetActiveAsync(studentId, target.Id, isActive: true);

        Assert.True(result.IsSuccess);
        Assert.True(target.IsActive);
        Assert.False(otherActive.IsActive);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetActive_ActivatingIncompleteScale_FailsWithValidation()
    {
        var empty = new GradeScale("Empty", Guid.NewGuid());
        _repo.Setup(r => r.GetByIdForStudentAsync(empty.Id, empty.StudentId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        var result = await _service.SetActiveAsync(empty.StudentId!.Value, empty.Id, isActive: true);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.False(empty.IsActive);
    }

    [Fact]
    public async Task Delete_RemovesScale()
    {
        var scale = new GradeScale("Old", Guid.NewGuid());
        _repo.Setup(r => r.GetByIdForStudentAsync(scale.Id, scale.StudentId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scale);

        var result = await _service.DeleteAsync(scale.StudentId!.Value, scale.Id);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.Remove(scale), Times.Once);
    }

    [Fact]
    public async Task GetSystemDefault_WhenMissing_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetSystemDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GradeScale?)null);

        var result = await _service.GetSystemDefaultAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    private static GradeScale CreateActiveScaleWithDefinitions(params (string Name, int Min, int Max)[] definitions)
    {
        var scale = new GradeScale("Scale", Guid.NewGuid());
        foreach (var (name, min, max) in definitions)
        {
            scale.AddDefinition(name, min, max, 3m);
        }
        scale.EnsureValid();
        return scale;
    }
}
