using GPAHub.Domain.Constants;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class Plan
{
    public const string FreeName = "Free";

    public const string PremiumName = "Premium";

    private readonly HashSet<string> _features = new(StringComparer.OrdinalIgnoreCase);

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<string> Features => _features;

    private Plan()
    {
        Name = string.Empty;
    }

    public Plan(string name, IEnumerable<string>? features = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Plan name is required.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();

        if (features is not null)
        {
            foreach (var feature in features)
            {
                AddFeature(feature);
            }
        }
    }

    public static Plan Free() => new(FreeName, [FeatureFlags.BasicCalculations]);

    public static Plan Premium() => new(PremiumName,
    [
        FeatureFlags.BasicCalculations,
        FeatureFlags.GradeCombinations,
        FeatureFlags.AdvancedAnalytics
    ]);

    public void AddFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
        {
            throw new DomainException("Feature flag cannot be empty.");
        }

        if (!_features.Add(feature.Trim().ToLowerInvariant()))
        {
            throw new DomainException($"Feature flag '{feature}' already exists on plan '{Name}'.");
        }
    }

    public bool HasFeature(string feature) =>
        _features.Contains(feature.Trim().ToLowerInvariant());
}
