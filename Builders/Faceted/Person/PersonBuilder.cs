
using Builders.FacetedBuilder.Person.Facets;

namespace Builders.FacetedBuilder.Person;

public class PersonBuilder
{
    protected Person person = new();

    public PersonAddressFacetBuilder Address => new(person);

    public PersonJobFacetBuilder Job => new(person);

    public static implicit operator Person(PersonBuilder pb)
    {
        return pb.person;
    }
}
