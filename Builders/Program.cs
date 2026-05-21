// See https://aka.ms/new-console-template for more information
using Builders;
using Builders.RecursiveGeneric.Animals.Duck;

Console.WriteLine("Hello, World!");



Duck duck = Duck.Builder.Origin("Germany")
                       .Wight(2.5)
                       .AgeInMonths(6)
                       .Color(DuckColors.White)
                       .Build();

Console.WriteLine();
