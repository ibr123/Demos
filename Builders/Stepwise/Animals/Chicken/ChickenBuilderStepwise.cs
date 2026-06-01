using Builders.Stepwise.Animals.Chicken.Interfaces;

namespace Builders.Stepwise.Animals.Chicken;

public class ChickenBuilderStepwise : IAggrigateChickenBuilder
{
    //this is an instante of ChickenBuilderStepwise

    private Chicken chicken = new();

    // Explicit interface implementations: these methods are NOT part of the
    // concrete type's public surface, so they can only be called through the
    // matching step-interface reference — which you only get by walking the
    // chain in order. This closes the `new ChickenBuilderStepwise().Age(...)` hole.

    IChickenkColorBuilder IChickenOriginBuilder.OfOrigin(string? origin)
    {
        chicken.Origin = origin;
        return this;
    }

    IChickenWeightBuilder IChickenkColorBuilder.Color(AnimalColors color)
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
