using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Services;
using GPAHub.Domain.Entities;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class ScaleResolverTests
{
    private readonly Mock<IGradeScaleRepository> _repo = new();
    private readonly Guid StudentId = Guid.NewGuid();

    [Fact]
    public async Task CustomDefinitions_BuildTransientScale_TakingPrecedence()
    {
        var custom = new List<SaveGradeDefinitionDto> { new("S", 0, 100, 5m) };

        var result = await ScaleResolver.ResolveAsync(
            _repo.Object, StudentId, custom, scaleId: null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.GetMaxGpaPoints());
        _repo.Verify(r => r.GetByIdForStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvalidCustomDefinitions_ReturnValidationFailure()
    {
        var custom = new List<SaveGradeDefinitionDto>
        {
            new("A", 90, 100, 4m),
            new("B", 95, 99, 3m)
        };

        var result = await ScaleResolver.ResolveAsync(_repo.Object, StudentId, custom, null, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ExplicitScaleId_FetchesOwnedScale()
    {
        var scaleId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdForStudentAsync(scaleId, StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GradeScale("Owned", StudentId));

        var result = await ScaleResolver.ResolveAsync(_repo.Object, StudentId, null, scaleId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Owned", result.Value.Name);
    }

    [Fact]
    public async Task ScaleId_WithoutAuthenticatedStudent_IsRejected()
    {
        var result = await ScaleResolver.ResolveAsync(_repo.Object, null, null, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task FallsBackToActiveScale_ThenSystemDefault()
    {
        var active = new GradeScale("Active", StudentId);
        active.AddDefinition("A", 50, 100, 4m);
        _repo.Setup(r => r.GetActiveForStudentAsync(StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        var withStudent = await ScaleResolver.ResolveAsync(_repo.Object, StudentId, null, null, CancellationToken.None);
        Assert.Equal("Active", withStudent.Value.Name);

        var fallback = new GradeScale("SystemDefault", null);
        fallback.AddDefinition("P", 0, 100, 1m);
        _repo.Setup(r => r.GetSystemDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fallback);

        var anonymous = await ScaleResolver.ResolveAsync(_repo.Object, null, null, null, CancellationToken.None);
        Assert.Equal("SystemDefault", anonymous.Value.Name);
    }

    [Fact]
    public async Task NothingAvailable_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetActiveForStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GradeScale?)null);
        _repo.Setup(r => r.GetSystemDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GradeScale?)null);

        var result = await ScaleResolver.ResolveAsync(_repo.Object, StudentId, null, null, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
