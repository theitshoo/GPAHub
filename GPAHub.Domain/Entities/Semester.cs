using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class Semester
{
    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public string Name { get; private set; }

    private Semester()
    {
        Name = string.Empty;
    }

    public Semester(Guid studentId, string name)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Semester name is required.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        Name = name.Trim();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Semester name is required.");
        }

        Name = name.Trim();
    }
}
