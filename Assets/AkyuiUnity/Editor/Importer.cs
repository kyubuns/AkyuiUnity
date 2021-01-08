using System.Collections.Generic;
using System.IO;
using AkyuiUnity.Editor.MiniJSON;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static void Import(string[] filePaths, AkyuiImportSettings settings)
        {
            foreach (var filePath in filePaths)
            {
                using (var zipFile = new ZipFile(filePath))
                {
                    var layoutJson = GetJson(zipFile, "layout.json");
                    foreach (var key in layoutJson.Keys)
                    {
                        Debug.Log($"{key} = {layoutJson[key]}");
                    }
                }
            }
        }

        private static Dictionary<string, object> GetJson(ZipFile zipFile, string name)
        {
            var findPath = Path.Combine(Path.GetFileNameWithoutExtension(zipFile.Name), name);
            var layoutJson = zipFile.FindEntry(findPath, true);

            var stream = zipFile.GetInputStream(layoutJson);
            using (var reader = new StreamReader(stream))
            {
                var jsonString = reader.ReadToEnd();
                var json = (Dictionary<string, object>) Json.Deserialize(jsonString);
                return json;
            }
        }
    }
}