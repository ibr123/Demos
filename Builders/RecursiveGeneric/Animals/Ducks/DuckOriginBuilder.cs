namespace Builders.RecursiveGeneric.Animals.Ducks;

public class DuckOriginBuilder<Self> : DuckAgeBuilder<Self>
    where Self : DuckOriginBuilder<Self>
{
    public Self Origin(string origin)
    {
        duck.Origin = origin;
        ///just for clarifications
        DuckOriginBuilder<Self> _duck2 = this;
        Self _duck = (Self)this;
        return _duck;
    }
}
