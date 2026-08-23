using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class SemesterTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        var studentId = Guid.NewGuid();
        var semester = new Semester(studentId, "Fall 2026");

        Assert.NotEqual(Guid.Empty, semester.Id);
        Assert.Equal(studentId, semester.StudentId);
        Assert.Equal("Fall 2026", semester.Name);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var semester = new Semester(Guid.NewGuid(), "  Spring 2027  ");

        Assert.Equal("Spring 2027", semester.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        Assert.Throws<DomainException>(() => new Semester(Guid.NewGuid(), name!));
    }
}
