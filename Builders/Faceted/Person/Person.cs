
namespace Builders.FacetedBuilder.Person;

public class Person
{
    public string? StreetName { get; set; }
    public string? PostCode { get; set; }
    public string? City { get; set; }
    public string? CompanyName { get; set; }
    public string? JobField { get; set; }
    public int AnnualIncome { get; set; }

    public override string ToString()
    {
        return
        "Facated Builder Example"
        + "\n" +
        $"{nameof(StreetName)}: {StreetName}, {nameof(PostCode)}: {PostCode}, {nameof(City)}: {City}, "
        + "\n" +
        $"{nameof(CompanyName)}: {CompanyName}, {nameof(JobField)}: {JobField}, {nameof(AnnualIncome)}: {AnnualIncome}";
    }
}
