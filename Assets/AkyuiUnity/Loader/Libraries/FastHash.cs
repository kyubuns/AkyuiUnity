using System.Collections.Generic;

namespace AkyuiUnity.Loader
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

        public static uint CalculateHash(IEnumerable<uint> read)
        {
            var hashedValue = 2654435761;
            foreach (var t in read)
            {
                hashedValue += t;
                hashedValue *= 2654435761;
            }
            return hashedValue;
        }

        public static uint CalculateHash(IEnumerable<byte> read)
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