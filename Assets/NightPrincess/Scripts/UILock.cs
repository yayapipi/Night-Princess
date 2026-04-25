namespace NightPrincess
{
    public static class UILock
    {
        private static int count;
        public static bool Active { get { return count > 0; } }
        public static int Count { get { return count; } }
        public static void Push() { count++; }
        public static void Pop() { if (count > 0) count--; }
        public static void Reset() { count = 0; }
    }
}
