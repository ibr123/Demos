
namespace Builders.Stepwise.Animals.Chicken.Interfaces;

public interface IAggregateChickenBuilder : IChickenBuilder,
                                            IChickenAgeBuilder,
                                            IChickenWeightBuilder,
                                            IChickenColorBuilder,
                                            IChickenOriginBuilder
                                            
{
}
