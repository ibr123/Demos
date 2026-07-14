namespace Builders.RecursiveGeneric.Animals.Ducks;

public class DuckWeightBuilder<Self> : DuckOriginBuilder<Self>
    where Self : DuckWeightBuilder<Self>
{
    public Self Wight(double weight)
    {
        duck.Weight = weight;
        ///just for clarifications
        DuckWeightBuilder<Self> _duck2 = this;
        Self _duck = (Self)this;
        return _duck;
    }
}
