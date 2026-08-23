using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class StudentTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        var student = new Student("Hesham", "Theitshoo@Example.com");

        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal("Hesham", student.Name);
        Assert.Equal("theitshoo@example.com", student.Email);
        Assert.Null(student.CurrentGpa);
        Assert.Null(student.CompletedCreditHours);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        Assert.Throws<DomainException>(() => new Student(name!, "student@example.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingEmail_Throws(string? email)
    {
        Assert.Throws<DomainException>(() => new Student("Name", email!));
    }

    [Fact]
    public void Constructor_NormalizesEmailToLowercaseAndTrims()
    {
        var student = new Student("Name", "  Mixed@Case.COM ");

        Assert.Equal("mixed@case.com", student.Email);
    }

    [Fact]
    public void UpdateBaseline_SetsBothValues()
    {
        var student = CreateStudent();

        student.UpdateBaseline(3.25m, 45m);

        Assert.Equal(3.25m, student.CurrentGpa);
        Assert.Equal(45m, student.CompletedCreditHours);
    }

    [Fact]
    public void UpdateBaseline_AllowsZeroHoursForFreshman()
    {
        var student = CreateStudent();

        student.UpdateBaseline(0m, 0m);

        Assert.Equal(0m, student.CurrentGpa);
        Assert.Equal(0m, student.CompletedCreditHours);
    }

    [Fact]
    public void UpdateBaseline_WithNegativeGpa_Throws()
    {
        var student = CreateStudent();

        Assert.Throws<DomainException>(() => student.UpdateBaseline(-0.1m, 30m));
    }

    [Fact]
    public void UpdateBaseline_WithNegativeHours_Throws()
    {
        var student = CreateStudent();

        Assert.Throws<DomainException>(() => student.UpdateBaseline(3.0m, -5m));
    }

    [Fact]
    public void ClearBaseline_NullsBothValues()
    {
        var student = CreateStudent();
        student.UpdateBaseline(3.25m, 45m);

        student.ClearBaseline();

        Assert.Null(student.CurrentGpa);
        Assert.Null(student.CompletedCreditHours);
    }

    [Fact]
    public void SetPasswordHash_StoresValue()
    {
        var student = CreateStudent();

        student.SetPasswordHash("hashed-value");

        Assert.Equal("hashed-value", student.PasswordHash);
    }

    [Fact]
    public void SetPasswordHash_WithEmpty_Throws()
    {
        var student = CreateStudent();

        Assert.Throws<DomainException>(() => student.SetPasswordHash(""));
    }

    [Fact]
    public void Rename_WithValidName_AppliesChange()
    {
        var student = CreateStudent();

        student.Rename("New Name");

        Assert.Equal("New Name", student.Name);
    }

    [Fact]
    public void Rename_WithEmpty_Throws()
    {
        var student = CreateStudent();

        Assert.Throws<DomainException>(() => student.Rename(" "));
    }

    private static Student CreateStudent() => new("Student", "student@example.com");
}
