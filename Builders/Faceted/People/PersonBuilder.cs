
using Builders.Faceted.People.Facets;

namespace Builders.Faceted.People;

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
