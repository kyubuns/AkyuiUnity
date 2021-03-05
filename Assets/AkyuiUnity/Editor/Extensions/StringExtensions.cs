namespace AkyuiUnity.Editor.Extensions
{
    public static class StringExtensions
    {
        public static string ToSafeString(this string self)
        {
            return self.Replace("/", "");
        }
    }
}