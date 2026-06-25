using Builders;
using Builders.Functional.Shapes;
using Builders.Functional.Shapes.Plugins;
using Builders.RecursiveGeneric.Animals.Duck;
using Builders.RecursiveGeneric.SmartEnum;
using Builders.Stepwise.Animals.Chicken;

Console.WriteLine("Hello, World!");


// Implementation of functional builder
ShapeBuilder shapeBuilder = new();

Shape circle = shapeBuilder.DefineShape("Circle").CalculateCircleArea(5).CalculateCirculePerimeter(5).Build();

Shape triangle = shapeBuilder.DefineShape("Triangle").CalculateTriangleArea(6, 3).CalculateTrianglePerimeter(4, 5, 7).Build();

// Implementation of stepwise builder
Duck duck = Duck.Builder.Origin("Germany")
                       .Wight(2.5)
                       .AgeInMonths(6)
                       .Color(AnimalColors.White)
                       .Build();

// Implementation of Recursive Generic
Chicken chicken = Chicken.ChickenBuilder.Origin("Japan")
                                        .Color(AnimalColors.Black)
                                        .Weight(5.5)
                                        .Age(3)
                                        .Build();

// This doesn't compile — OfOrigin is the only method available on the entry point:
//Chicken chicken2 = Chicken.ChickenBuilder.Age(2).Build();

Console.WriteLine();

// ---- Enumeration<Self> demo (CRTP applied to a strongly-typed enum) ----
Console.WriteLine("All order statuses:");
foreach (OrderStatus status in OrderStatus.GetAll())
{
    Console.WriteLine($"  {status.Id} - {status.Name}");
}

OrderStatus current = OrderStatus.Pending;
Console.WriteLine($"\nCurrent status: {current}");
Console.WriteLine($"Next status:    {current.Next()}");

OrderStatus lookedUpById = OrderStatus.FromId(3);
OrderStatus lookedUpByName = OrderStatus.FromName("Delivered");
Console.WriteLine($"\nLookup by Id(3):         {lookedUpById}");
Console.WriteLine($"Lookup by Name(Delivered): {lookedUpByName}");

Console.WriteLine($"\nPending == Pending? {OrderStatus.Pending.Equals(OrderStatus.Pending)}");
Console.WriteLine($"Pending == Paid?    {OrderStatus.Pending.Equals(OrderStatus.Paid)}");
