using System.Reflection;

namespace Builders.RecursiveGeneric.SmartEnum;

public abstract class Enumeration<Self> : IComparable
    where Self : Enumeration<Self>
{
    public int Id { get; }
    public string Name { get; }

    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => Name;

    public static IEnumerable<Self> GetAll()
    {
        return typeof(Self)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => typeof(Self).IsAssignableFrom(f.FieldType))
            .Select(f => (Self)f.GetValue(null)!);
    }

    public static Self FromId(int id)
    {
        Self? match = GetAll().FirstOrDefault(item => item.Id == id);
        if (match is null)
            throw new InvalidOperationException($"No {typeof(Self).Name} with Id = {id}");
        return match;
    }

    public static Self FromName(string name)
    {
        Self? match = GetAll().FirstOrDefault(item => item.Name == name);
        if (match is null)
            throw new InvalidOperationException($"No {typeof(Self).Name} with Name = '{name}'");
        return match;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration<Self> other) return false;
        return GetType() == obj.GetType() && Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object? other) => Id.CompareTo(((Enumeration<Self>)other!).Id);

    public Self Next()
    {
        List<Self> all = GetAll().OrderBy(e => e.Id).ToList();
        int currentIndex = all.FindIndex(e => e.Id == Id);
        return all[(currentIndex + 1) % all.Count];
    }
}
