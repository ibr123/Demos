namespace Builders.Functional.Shapes;

public sealed class ShapeBuilder
{
    public readonly List<Action<Shape>> ShapeBuildingActions = [];

    public ShapeBuilder DefineShape(string shapeType)
    {
        ShapeBuildingActions.Add(shapeTypeBuilder =>
        {
            shapeTypeBuilder.ShapeType = shapeType;
        });

        return this;
    }

    public Shape Build()
    {
        Shape shape = new();

        ShapeBuildingActions.ForEach(buildingAction => buildingAction(shape));
        ShapeBuildingActions.Clear();   // reset the recipe so the next product starts clean
        return shape;
    }
}
