namespace Builders.RecursiveGeneric.Animals.Ducks;

// can not create instance from DuckBuildAggregator<Self> so DuckBuilder is necessary, look "Builder" at Duck.cs
public sealed class DuckBuilder : DuckBuildAggregator<DuckBuilder>
{
    
}
