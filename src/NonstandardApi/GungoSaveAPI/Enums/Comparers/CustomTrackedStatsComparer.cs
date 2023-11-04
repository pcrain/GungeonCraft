namespace SaveAPI
{
    public class CustomTrackedStatsComparer : IEqualityComparer<CustomTrackedStats>
    {
        public bool Equals(CustomTrackedStats x, CustomTrackedStats y)
        {
            return x == y;
        }

        public int GetHashCode(CustomTrackedStats obj)
        {
            return (int)obj;
        }
    }
}
