
namespace Builders.Stepwise.Animals.Chicken.Interfaces;

public interface IAggrigateChickenBuilder : IChickenBuilder,
                                            IChickenAgeBuilder, 
                                            IChickenWeightBuilder, 
                                            IChickenkColorBuilder, 
                                            IChickenOriginBuilder
                                            
{
}
