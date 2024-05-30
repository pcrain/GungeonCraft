namespace SaveAPI;

public class CustomTrackedMaximumsComparer : IEqualityComparer<CustomTrackedMaximums>
{
    public bool Equals(CustomTrackedMaximums x, CustomTrackedMaximums y)
    {
        return x == y;
    }

    public int GetHashCode(CustomTrackedMaximums obj)
    {
        return (int)obj;
    }
}
