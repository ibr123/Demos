
using Builders.FacetedBuilder.Person.Facets;

namespace Builders.FacetedBuilder.Person;

public class PersonBuilder
{
    protected Person person = new();

    public PersonAddressFacetBuilder Address => new();

    public PersonJobFacetBuilder Job => new();

    public static implicit operator Person(PersonBuilder pb)
    {
        return pb.person;
    }
}
