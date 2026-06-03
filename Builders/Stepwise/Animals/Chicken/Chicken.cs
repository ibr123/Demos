using Builders.Stepwise.Animals.Chicken.Interfaces;

namespace Builders.Stepwise.Animals.Chicken;

public class Chicken
{
    public int AgeInMonths { get; set; }

    public double Weight { get; set; }

    public string? Origin { get; set; }

    public AnimalColors Color { get; set; }

    // Typed as the first step only, so the caller must start with Origin.
    // A property (=> new) also hands out a fresh builder each time, avoiding shared state.
    public static IChickenOriginBuilder ChickenBuilder => new ChickenStepwiseBuilder();
}
