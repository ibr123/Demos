namespace Builders.RecursiveGeneric.Animals.Duck;

public class DuckColorBuilder<Self> : DuckWeightBuilder<Self>
    where Self : DuckColorBuilder<Self>
{
    public Self Color(AnimalColors color)
    {
        duck.Color = color;
        ///just for clarifications
        DuckColorBuilder<Self> _duck2 = this;
        Self _duck = (Self)this;
        return _duck;
    }
}
