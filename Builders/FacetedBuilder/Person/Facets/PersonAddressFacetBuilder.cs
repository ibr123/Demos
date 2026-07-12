
namespace Builders.FacetedBuilder.Person.Facets;

public class PersonAddressFacetBuilder : PersonBuilder
{
    public PersonAddressFacetBuilder LivesAt(string city, string postCode, string streetName)
    {
        person.City = city;
        person.PostCode = postCode;
        person.StreetName = streetName;
        return this;
    }
}
