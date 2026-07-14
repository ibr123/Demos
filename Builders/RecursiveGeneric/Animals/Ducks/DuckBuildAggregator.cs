namespace Builders.RecursiveGeneric.Animals.Ducks;

public class DuckBuildAggregator<Self> : DuckColorBuilder<Self>
    where Self : DuckBuildAggregator<Self>
{

}
