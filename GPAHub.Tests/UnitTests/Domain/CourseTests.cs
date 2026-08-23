using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class CourseTests
{
    [Fact]
    public void CreateNumeric_WithValidData_SetsMarkAndType()
    {
        var course = Course.CreateNumeric(Guid.NewGuid(), "Calculus", "MATH101", 3m, 87);

        Assert.Equal(GradeInputType.NumericMark, course.InputType);
        Assert.Equal(87, course.NumericMark);
        Assert.Null(course.LetterGrade);
        Assert.Equal("Calculus", course.Name);
        Assert.Equal("MATH101", course.Code);
        Assert.Equal(3m, course.CreditHours.Value);
    }

    [Fact]
    public void CreateLetterGrade_WithValidData_SetsGradeAndType()
    {
        var course = Course.CreateLetterGrade(Guid.NewGuid(), "Physics", null, 4m, "A-");

        Assert.Equal(GradeInputType.LetterGrade, course.InputType);
        Assert.Equal("A-", course.LetterGrade);
        Assert.Null(course.NumericMark);
        Assert.Null(course.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateNumeric_WithMissingName_Throws(string? name)
    {
        Assert.Throws<DomainException>(
            () => Course.CreateNumeric(Guid.NewGuid(), name!, null, 3m, 80));
    }

    [Fact]
    public void CreateNumeric_WithZeroCreditHours_Throws()
    {
        Assert.Throws<DomainException>(
            () => Course.CreateNumeric(Guid.NewGuid(), "Math", null, 0m, 80));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreateNumeric_WithMarkOutsideRange_Throws(int mark)
    {
        Assert.Throws<DomainException>(
            () => Course.CreateNumeric(Guid.NewGuid(), "Math", null, 3m, mark));
    }

    [Fact]
    public void CreateLetterGrade_WithEmptyGrade_Throws()
    {
        Assert.Throws<DomainException>(
            () => Course.CreateLetterGrade(Guid.NewGuid(), "Physics", null, 3m, "  "));
    }

    [Fact]
    public void UpdateNumeric_ChangesMark_AndKeepsTypeConsistent()
    {
        var studentId = Guid.NewGuid();
        var course = Course.CreateLetterGrade(studentId, "Art", null, 2m, "B");

        course.UpdateAsNumeric(91);

        Assert.Equal(GradeInputType.NumericMark, course.InputType);
        Assert.Equal(91, course.NumericMark);
        Assert.Null(course.LetterGrade);
    }

    [Fact]
    public void UpdateLetter_ChangesGrade_AndKeepsTypeConsistent()
    {
        var studentId = Guid.NewGuid();
        var course = Course.CreateNumeric(studentId, "Math", null, 3m, 70);

        course.UpdateAsLetter("C+");

        Assert.Equal(GradeInputType.LetterGrade, course.InputType);
        Assert.Equal("C+", course.LetterGrade);
        Assert.Null(course.NumericMark);
    }

    [Fact]
    public void UpdateDetails_ChangesNameCodeAndCredits()
    {
        var studentId = Guid.NewGuid();
        var course = Course.CreateNumeric(studentId, "Math", "M1", 3m, 70);

        course.UpdateDetails("Advanced Math", "  MATH200  ", 4m);

        Assert.Equal("Advanced Math", course.Name);
        Assert.Equal("MATH200", course.Code);
        Assert.Equal(4m, course.CreditHours.Value);
    }

    [Fact]
    public void AssignToSemester_SetsSemesterId()
    {
        var semesterId = Guid.NewGuid();
        var course = Course.CreateNumeric(Guid.NewGuid(), "Math", null, 3m, 70);

        course.AssignToSemester(semesterId);

        Assert.Equal(semesterId, course.SemesterId);
    }

    [Fact]
    public void RemoveFromSemester_ClearsSemesterId()
    {
        var course = Course.CreateNumeric(Guid.NewGuid(), "Math", null, 3m, 70);
        course.AssignToSemester(Guid.NewGuid());

        course.RemoveFromSemester();

        Assert.Null(course.SemesterId);
    }
}
