
namespace Builders.Stepwise.Animals.Chickens.Interfaces;

public interface IAggregateChickenBuilder : IChickenBuilder,
                                            IChickenAgeBuilder,
                                            IChickenWeightBuilder,
                                            IChickenColorBuilder,
                                            IChickenOriginBuilder
                                            
{
}
