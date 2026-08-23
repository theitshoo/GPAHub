using GPAHub.Domain.Constants;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class GradeScale
{
    private readonly List<GradeDefinition> _definitions = [];

    public Guid Id { get; private set; }

    public Guid? StudentId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public bool EnforceFullCoverage { get; private set; }

    public IReadOnlyList<GradeDefinition> Definitions => _definitions.AsReadOnly();

    private GradeScale()
    {
        Name = string.Empty;
    }

    public GradeScale(string name, Guid? studentId, string? description = null, bool enforceFullCoverage = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Grade scale name is required.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        EnforceFullCoverage = enforceFullCoverage;
    }

    public GradeDefinition AddDefinition(string name, int minMark, int maxMark, decimal points)
    {
        EnsureNameAvailable(name, definitionIdToExclude: null);

        var definition = new GradeDefinition(name, minMark, maxMark, points);

        EnsureNoOverlap(definition, definitionIdToExclude: null);

        _definitions.Add(definition);

        return definition;
    }

    public void UpdateDefinition(Guid definitionId, string name, int minMark, int maxMark, decimal points)
    {
        var definition = FindOrThrow(definitionId);

        EnsureNameAvailable(name, definitionIdToExclude: definitionId);

        definition.Update(name, minMark, maxMark, points);

        EnsureNoOverlap(definition, definitionIdToExclude: definitionId);
    }

    public void RemoveDefinition(Guid definitionId)
    {
        var definition = FindOrThrow(definitionId);

        _definitions.Remove(definition);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public GradeDefinition? FindDefinitionForMark(int mark) =>
        _definitions.FirstOrDefault(d => mark >= d.MinMark && mark <= d.MaxMark);

    public GradeDefinition? FindDefinitionForGradeName(string gradeName)
    {
        if (string.IsNullOrWhiteSpace(gradeName))
        {
            return null;
        }

        return _definitions.FirstOrDefault(d => d.HasSameNameAs(gradeName));
    }

    public decimal GetMaxGpaPoints()
    {
        if (_definitions.Count == 0)
        {
            throw new DomainException("Cannot determine maximum GPA points: scale has no grade definitions.");
        }

        return _definitions.Max(d => d.Points);
    }

    public void EnsureValid()
    {
        var errors = new List<string>();

        if (_definitions.Count == 0)
        {
            errors.Add("At least one grade definition is required.");
            throw new InvalidGradeScaleException(errors);
        }

        if (EnforceFullCoverage && !IsFullyCovered())
        {
            errors.Add($"Grade definitions must cover the full mark range {MarkRange.AbsoluteMinimum}-{MarkRange.AbsoluteMaximum} without gaps.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidGradeScaleException(errors);
        }
    }

    private bool IsFullyCovered()
    {
        var ordered = _definitions.OrderBy(d => d.MinMark).ToList();

        if (ordered[0].MinMark != MarkRange.AbsoluteMinimum ||
            ordered[^1].MaxMark != MarkRange.AbsoluteMaximum)
        {
            return false;
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].MinMark > ordered[i - 1].MaxMark + 1)
            {
                return false;
            }
        }

        return true;
    }

    private GradeDefinition FindOrThrow(Guid definitionId)
    {
        var definition = _definitions.SingleOrDefault(d => d.Id == definitionId);

        if (definition is null)
        {
            throw new DomainException("Grade definition was not found in this scale.");
        }

        return definition;
    }

    private void EnsureNameAvailable(string name, Guid? definitionIdToExclude)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Grade name is required.");
        }

        if (_definitions.Any(d => d.Id != definitionIdToExclude && d.HasSameNameAs(name)))
        {
            throw new DomainException($"Grade name '{name.Trim()}' already exists in this scale.");
        }
    }

    private void EnsureNoOverlap(GradeDefinition candidate, Guid? definitionIdToExclude)
    {
        var conflicting = _definitions.FirstOrDefault(
            d => d.Id != definitionIdToExclude && d.Overlaps(candidate));

        if (conflicting is not null)
        {
            throw new DomainException(
                $"Grade '{candidate.Name}' ({candidate.MinMark}-{candidate.MaxMark}) overlaps with '{conflicting.Name}' ({conflicting.MinMark}-{conflicting.MaxMark}).");
        }
    }
}
