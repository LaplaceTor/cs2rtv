namespace Cs2Rtv;

public class Map(int id, string name, int tier) {
    public int id { get; } = id;
    public string name { get; } = name;
    public int tier { get; } = tier;

    public override string ToString() {
        return $"{nameof(Map)}({nameof(id)}: {id}, {nameof(name)}: {name}, {nameof(tier)}: {tier})";
    }
}