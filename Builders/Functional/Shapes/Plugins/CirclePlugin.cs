namespace Builders.Functional.Shapes.Plugins;

public static class CirclePlugin
{
    public static ShapeBuilder ClaculateCircleArea(this ShapeBuilder shapeBuilder, float radius)
    {
        shapeBuilder.ShapeBuildingActions.Add(circle =>
        {
            circle.Area = Math.PI * Math.Pow(radius, 2);
        });

        return shapeBuilder;
    }

    public static ShapeBuilder CalculateCirculePerimeter(this ShapeBuilder shapeBuilder, float radius)
    {
        shapeBuilder.ShapeBuildingActions.Add(circle =>
        {
            circle.Perimeter = 2 * Math.PI * radius;
        });

        return shapeBuilder;
    }
}
