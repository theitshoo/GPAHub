using GPAHub.Application.Common;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.Validators;

namespace GPAHub.Tests.UnitTests.Validators;

public class GradeScaleValidatorsTests
{
    private readonly SaveGradeDefinitionDtoValidator _definitionValidator = new();
    private readonly CreateGradeScaleDtoValidator _createValidator = new();

    [Fact]
    public async Task Definition_WithValidData_Passes()
    {
        var dto = new SaveGradeDefinitionDto("A", 90, 100, 4.0m);

        var result = await _definitionValidator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Definition_WhenMinGreaterThanMax_FailsWithSpecificCode()
    {
        var dto = new SaveGradeDefinitionDto("A", 95, 90, 4.0m);

        var result = await _definitionValidator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "min_greater_than_max");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Definition_WithMarkOutsideRange_Fails(int mark)
    {
        var dto = new SaveGradeDefinitionDto("A", mark, mark, 4.0m);

        var result = await _definitionValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "mark_out_of_range");
    }

    [Fact]
    public async Task Definition_WithNegativePoints_Fails()
    {
        var dto = new SaveGradeDefinitionDto("A", 90, 100, -1m);

        var result = await _definitionValidator.ValidateAsync(dto);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateScale_WithEmptyName_Fails()
    {
        var dto = new CreateGradeScaleDto("", null);

        var result = await _createValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "scale_name_required");
    }

    [Fact]
    public void Result_OkCarriesValue_AndNoError()
    {
        var result = Result<int>.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_FailCarriesError_AndValueAccessThrows()
    {
        var result = Result<int>.Fail(Error.NotFound("x.not_found", "not found"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Result_SuccessCannotCarryError()
    {
        Assert.Throws<InvalidOperationException>(() => new BrokenSuccessResult());
    }

    private sealed class BrokenSuccessResult : Result
    {
        public BrokenSuccessResult() : base(true, Error.Validation("bad", "bad"))
        {
        }
    }
}
