using XdParser.Internal;

namespace XdParser
{
    public static class XdObjectParserExtension
    {
        public static XdColorValueJson GetFillColor(this XdObjectJson xdObject)
        {
            var color = new XdColorValueJson { R = 255, G = 255, B = 255 };
            if (xdObject.Style != null)
            {
                var fill = xdObject.Style.Fill;
                if (fill?.Color?.Value?.R != null)
                {
                    color.R = fill.Color.Value.R;
                    color.G = fill.Color.Value.G;
                    color.B = fill.Color.Value.B;
                }
            }
            return color;
        }

        public static string ToColorString(this XdColorValueJson colorValue)
        {
            return $"#{colorValue.R:x2}{colorValue.G:x2}{colorValue.B:x2}";
        }
    }
}