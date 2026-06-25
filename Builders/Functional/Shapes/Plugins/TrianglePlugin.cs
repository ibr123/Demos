namespace Builders.Functional.Shapes.Plugins;

public static class TrianglePlugin
{
    public static ShapeBuilder CalculateTriangleArea(this ShapeBuilder shapeBuilder, float height, float @base)
    {
        shapeBuilder.ShapeBuildingActions.Add(triangle =>
        {
            triangle.Area = height * @base / 2;
        });

        return shapeBuilder;
    }

    public static ShapeBuilder CalculateTrianglePerimeter(this ShapeBuilder shapeBuilder, float sideA, float sideB, float sideC)
    {
        shapeBuilder.ShapeBuildingActions.Add(triangle =>
        {
            triangle.Perimeter = sideA + sideB + sideC;
        });

        return shapeBuilder;
    }
}
