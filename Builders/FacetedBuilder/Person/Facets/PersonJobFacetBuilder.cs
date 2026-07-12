
namespace Builders.FacetedBuilder.Person.Facets;

public class PersonJobFacetBuilder : PersonBuilder
{
    
    public PersonJobFacetBuilder WorksAt(string companyName)
    {
        person.CompanyName = companyName;
        return this;
    }

    public PersonJobFacetBuilder WorksAs(string profession)
    {
        person.JobField = profession;
        return this;
    }

    public PersonJobFacetBuilder Makes(int annualIncome)
    {
        person.AnnualIncome = annualIncome;
        return this;
    }
}
