namespace Scripts.Utility
{
    public static class CompareExtensions
    {
        public static int CompareTo(this float value1, float value2)
        {
            if (value1 < value2)
                return -1;
            else if (value1 > value2)
                return 1;
            else
                return 0;
        }
    }
}

