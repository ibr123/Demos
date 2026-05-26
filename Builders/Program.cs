// See https://aka.ms/new-console-template for more information
using Builders;
using Builders.RecursiveGeneric.Animals.Duck;
using Builders.RecursiveGeneric.SmartEnum;

Console.WriteLine("Hello, World!");



Duck duck = Duck.Builder.Origin("Germany")
                       .Wight(2.5)
                       .AgeInMonths(6)
                       .Color(DuckColors.White)
                       .Build();

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

OrderStatus lookedUpById   = OrderStatus.FromId(3);
OrderStatus lookedUpByName = OrderStatus.FromName("Delivered");
Console.WriteLine($"\nLookup by Id(3):         {lookedUpById}");
Console.WriteLine($"Lookup by Name(Delivered): {lookedUpByName}");

Console.WriteLine($"\nPending == Pending? {OrderStatus.Pending.Equals(OrderStatus.Pending)}");
Console.WriteLine($"Pending == Paid?    {OrderStatus.Pending.Equals(OrderStatus.Paid)}");
