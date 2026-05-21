namespace Builders.RecursiveGeneric.Animals.Duck;

public class DuckBuildAggregator<Self> : DuckColorBuilder<Self>
    where Self : DuckBuildAggregator<Self>
{

}
