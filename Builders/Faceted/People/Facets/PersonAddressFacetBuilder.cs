
namespace Builders.Faceted.People.Facets;

public class PersonAddressFacetBuilder : PersonBuilder
{
    public PersonAddressFacetBuilder(Person person)
    {
        this.person = person;
    }

    public PersonAddressFacetBuilder At(string city, string postCode, string streetName)
    {
        person.City = city;
        person.PostCode = postCode;
        person.StreetName = streetName;
        return this;
    }
}
