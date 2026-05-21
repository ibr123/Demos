namespace Builders.RecursiveGeneric.Animals.Duck;

//used for returning Duck instance
public abstract class DuckFeatures
{
    protected Duck duck = new();
    public Duck Build()
    {
        return duck;
    }
}
