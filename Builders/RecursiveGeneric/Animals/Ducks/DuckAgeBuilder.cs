namespace Builders.RecursiveGeneric.Animals.Ducks;

public class DuckAgeBuilder<Self> : DuckFeatures 
    where Self : DuckAgeBuilder<Self>
{
    public Self AgeInMonths(int ageInMonths)
    {
        duck.AgeInMonths = ageInMonths;
        ///just for clarifications
        DuckAgeBuilder<Self> _duck2 = this;
        Self _duck = (Self)this;
        return _duck;
    }
}
