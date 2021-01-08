using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor.MiniJSON;
using UnityEngine;
using UnityEngine.UI;
using ICSharpCode.SharpZipLib.Zip;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static void Import(string[] filePaths, AkyuiImportSettings settings)
        {
            foreach (var filePath in filePaths)
            {
                Debug.Log($"Import Start: {filePath}");
                using (var zipFile = new ZipFile(filePath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(zipFile.Name);
                    var layoutJson = GetJson(zipFile, Path.Combine(fileName, "layout.json"));
                    var elements = (List<object>) layoutJson["elements"];
                    CreateGameObject(elements.Select(x => (Dictionary<string, object>) x).ToArray());
                }
                Debug.Log($"Import Finish: {filePath}");
            }
        }

        private static Dictionary<string, object> GetJson(ZipFile zipFile, string name)
        {
            var layoutJson = zipFile.FindEntry(name, true);

            var stream = zipFile.GetInputStream(layoutJson);
            using (var reader = new StreamReader(stream))
            {
                var jsonString = reader.ReadToEnd();
                var json = (Dictionary<string, object>) Json.Deserialize(jsonString);
                return json;
            }
        }

        private static GameObject CreateGameObject(Dictionary<string, object>[] elements)
        {
            var idToElement = new Dictionary<int, Dictionary<string, object>>();

            var rootId = ToInt(elements[0]["id"]);
            foreach (var element in elements)
            {
                var id = ToInt(element["id"]);
                idToElement[id] = element;
            }

            return CreateGameObject(idToElement, rootId, null);
        }

        private static GameObject CreateGameObject(Dictionary<int, Dictionary<string, object>> idToElement, int id, Transform parent)
        {
            var element = idToElement[id];
            var name = ToString(element["name"]);
            var position = ToVector2(element["position"]);
            var size = ToVector2(element["size"]);
            var anchorMin = ToVector2(element["anchor_min"]);
            var anchorMax = ToVector2(element["anchor_max"]);
            var pivot = ToVector2(element["pivot"]);
            var children = ToIntArray(element["children"]);
            var components = ((List<object>) element["components"]).Select(x => (Dictionary<string, object>) x).ToArray();

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;

            foreach (var component in components)
            {
                CreateComponent(gameObject, component);
            }

            foreach (var child in children)
            {
                CreateGameObject(idToElement, child, gameObject.transform);
            }

            return gameObject;
        }

        private static void CreateComponent(GameObject gameObject, Dictionary<string, object> component)
        {
            var type = ToString(component["type"]);
            if (type == "image")
            {
                var image = gameObject.AddComponent<Image>();
            }
            else if (type == "text")
            {
                var text = gameObject.AddComponent<Text>();
                text.text = ToString(component["text"]);
                text.fontSize = ToInt(component["size"]);
                text.color = ToColor(component["color"]);
                switch (ToString(component["align"]))
                {
                    case "middle_center":
                        text.alignment = TextAnchor.MiddleCenter;
                        break;

                    default:
                        Debug.LogWarning($"Unknown align {ToString(component["align"])}");
                        break;
                }
            }
            else if (type == "button")
            {
                gameObject.AddComponent<Button>();
            }
            else
            {
                Debug.LogWarning($"Unknown component {type}");
            }
        }

        private static string ToString(object o)
        {
            return (string) o;
        }

        private static int ToInt(object o)
        {
            if (o is long l) return (int) l;
            if (o is double d) return (int) d;
            throw new Exception($"{o} is {o.GetType()}");
        }

        private static int[] ToIntArray(object o)
        {
            var a = (List<object>) o;
            return a.Select(x =>
            {
                if (x is long l) return (int) l;
                if (x is double d) return (int) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
        }

        private static Color ToColor(object o)
        {
            var a = (List<object>) o;
            var b = a.Select(x =>
            {
                if (x is long l) return (byte) l;
                if (x is double d) return (byte) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
            return new Color32(b[0], b[1], b[2], b[3]);
        }

        private static Vector2 ToVector2(object o)
        {
            var a = (List<object>) o;
            var b = a.Select(x =>
            {
                if (x is long l) return (float) l;
                if (x is double d) return (float) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();

            return new Vector2(b[0], b[1]);
        }
    }
}