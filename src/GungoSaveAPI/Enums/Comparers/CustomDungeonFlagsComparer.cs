namespace SaveAPI;

public class CustomDungeonFlagsComparer : IEqualityComparer<CustomDungeonFlags>
{
    public bool Equals(CustomDungeonFlags x, CustomDungeonFlags y)
    {
        return x == y;
    }

    public int GetHashCode(CustomDungeonFlags obj)
    {
        return (int)obj;
    }
}
