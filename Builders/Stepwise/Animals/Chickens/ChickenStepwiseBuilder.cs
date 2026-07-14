using Builders.Stepwise.Animals.Chickens.Interfaces;

namespace Builders.Stepwise.Animals.Chickens;

public class ChickenStepwiseBuilder : IAggregateChickenBuilder
{
    private Chicken chicken = new();

    // Explicit interface implementations: these methods are NOT part of the
    // concrete type's public surface, so they can only be called through the
    // matching step-interface reference — which you only get by walking the
    // chain in order. This closes the `new ChickenStepwiseBuilder().Age(...)` hole.

    IChickenColorBuilder IChickenOriginBuilder.Origin(string? origin)
    {
        chicken.Origin = origin;
        return this;
    }

    IChickenWeightBuilder IChickenColorBuilder.Color(AnimalColors color)
    {
        chicken.Color = color;
        return this;
    }

    IChickenAgeBuilder IChickenWeightBuilder.Weight(double weight)
    {
        chicken.Weight = weight;
        return this;
    }

    IChickenBuilder IChickenAgeBuilder.Age(int age)
    {
        chicken.AgeInMonths = age;
        return this;
    }

    Chicken IChickenBuilder.Build()
    {
        return chicken;
    }
}
