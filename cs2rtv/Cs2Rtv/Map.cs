namespace Cs2Rtv;

public class Map
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Tier { get; set; }

    public Map(long id, string name, int tier)
    {
        Id = id;
        Name = name;
        Tier = tier;
    }

    public Map() { }

    public override string ToString()
    {
        return $"{nameof(Map)}({nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Tier)}: {Tier})";
    }
}