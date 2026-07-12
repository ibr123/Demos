
namespace Builders.FacetedBuilder.Person.Facets;

public class PersonJobFacetBuilder : PersonBuilder
{
    public PersonJobFacetBuilder(Person person)
    {
        this.person = person;
    }

    
    public PersonJobFacetBuilder At(string companyName)
    {
        person.CompanyName = companyName;
        return this;
    }

    public PersonJobFacetBuilder ProfessionAs(string profession)
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
