namespace AkyuiUnity.Xd.Libraries
{
    public static class FastHash
    {
        public static uint CalculateHash(string read)
        {
            var hashedValue = 2654435761;
            foreach (var t in read)
            {
                hashedValue += t;
                hashedValue *= 2654435761;
            }
            return hashedValue;
        }

        public static uint CalculateHash(uint[] read)
        {
            var hashedValue = 2654435761;
            foreach (var t in read)
            {
                hashedValue += t;
                hashedValue *= 2654435761;
            }
            return hashedValue;
        }
    }
}