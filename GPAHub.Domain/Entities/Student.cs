using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class Student
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string? PasswordHash { get; private set; }

    public decimal? CurrentGpa { get; private set; }

    public decimal? CompletedCreditHours { get; private set; }

    private Student()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public Student(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Student name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Student email is required.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Student name is required.");
        }

        Name = name.Trim();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
    }

    public void UpdateBaseline(decimal currentGpa, decimal completedCreditHours)
    {
        if (currentGpa < 0m)
        {
            throw new DomainException("Current GPA cannot be negative.");
        }

        if (completedCreditHours < 0m)
        {
            throw new DomainException("Completed credit hours cannot be negative.");
        }

        CurrentGpa = currentGpa;
        CompletedCreditHours = completedCreditHours;
    }

    public void ClearBaseline()
    {
        CurrentGpa = null;
        CompletedCreditHours = null;
    }
}
