namespace Builders.RecursiveGeneric.Animals.Duck;

public class Duck
{
    public int AgeInMonths { get; set; }

    public double Weight { get; set; }

    public string? Origin { get; set; }

    public DuckColors Color { get; set; }

    public static DuckBuilder Builder => new();
}
