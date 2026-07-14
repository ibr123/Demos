namespace Builders.RecursiveGeneric.Animals.Ducks;

public class Duck
{
    public int AgeInMonths { get; set; }

    public double Weight { get; set; }

    public string? Origin { get; set; }

    public AnimalColors Color { get; set; }

    public static DuckBuilder Builder => new();
}
