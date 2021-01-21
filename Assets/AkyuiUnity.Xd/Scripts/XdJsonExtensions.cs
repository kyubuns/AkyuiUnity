using System.Linq;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public static class XdJsonExtensions
    {
        public static string GetSimpleName(this XdObjectJson xdObjectJson)
        {
            if (xdObjectJson.Name == null) return string.Empty;
            return xdObjectJson.Name.Split('@')[0];
        }

        public static string[] GetParameters(this XdObjectJson xdObjectJson)
        {
            if (xdObjectJson.Name == null) return new string[] { };

            var e = xdObjectJson.Name.Split('@');
            if (e.Length <= 1) return new string[] { };

            return e[1].Split(',').Select(x => x.ToLowerInvariant().Trim()).ToArray();
        }
    }
}