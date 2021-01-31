using System.IO;
using System.Linq;
using UnityEditor;

namespace AkyuiUnity.Editor
{
    public static class AkyuiEditorUtil
    {
        public static string ToUniversalPath(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar.ToString(), "/");
        }

        public static void CreateDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path);
            CreateDirectory(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static string ValidFileName(string s)
        {
            return Path.GetInvalidFileNameChars().Aggregate(s, (current, c) => current.Replace(c, '_'));
        }
    }
}