using Builders.Stepwise.Animals.Chicken.Interfaces;

namespace Builders.Stepwise.Animals.Chicken;

public class ChickenBuilderStepwise : IAggrigateChickenBuilder
{
    //this is an instante of ChickenBuilderStepwise

    private Chicken chicken = new();

    public static ChickenBuilderStepwise ChickenBuilder = new();

    public  IChickenkColorBuilder OfOrigin(string? origin)
    {
        chicken.Origin = origin;
        return this;
    }

    public IChickenWeightBuilder Color(AnimalColors color)
    {
        chicken.Color = color;
        return this;
    }

    public IChickenAgeBuilder Weight(double weight)
    {
        chicken.Weight = weight;
        return this;
    }
    public IChickenBuilder Age(int age)
    {
        chicken.AgeInMonths = age;
        return this;
    }
    public Chicken Build()
    {
        return chicken;
    }
}
