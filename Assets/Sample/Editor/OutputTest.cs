using AkyuiUnity.Editor;
using AkyuiUnity.Loader;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Sample
{
    public static class OutputTest
    {
        [MenuItem("Test/SaveTest")]
        public static void SaveTest()
        {
            var loader = new AkyuiLoader("/Users/kyubuns/Downloads/SimpleButton.aky");
            Importer.Save(loader, "/Users/kyubuns/Desktop/Output.zip");
            Debug.Log($"Output");
        }
    }
}