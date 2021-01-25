using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Generator.InternalTrigger;
using UnityEngine;

namespace AkyuiUnity.Generator
{
    public static class AkyuiGenerator
    {
        public static (GameObject, long Hash) GenerateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo, IAkyuiGenerateTrigger[] triggers)
        {
            var triggersWithDefault = triggers.Concat(new[] { new DefaultGenerateTrigger() }).ToArray();
            var gameObject = CreateGameObject(assetLoader, layoutInfo, layoutInfo.Root, null, triggersWithDefault);
            return (gameObject, layoutInfo.Hash);
        }

        private static GameObject CreateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo, int eid, Transform parent, IAkyuiGenerateTrigger[] triggers)
        {
            (Vector2 Min, Vector2 Max) CalcAnchor(AnchorXType x, AnchorYType y)
            {
                var anchorMin = Vector2.zero;
                var anchorMax = Vector2.zero;

                switch (x)
                {
                    case AnchorXType.Left:
                        anchorMin.x = 0.0f;
                        anchorMax.x = 0.0f;
                        break;
                    case AnchorXType.Center:
                        anchorMin.x = 0.5f;
                        anchorMax.x = 0.5f;
                        break;
                    case AnchorXType.Right:
                        anchorMin.x = 1.0f;
                        anchorMax.x = 1.0f;
                        break;
                    case AnchorXType.Stretch:
                        anchorMin.x = 0.0f;
                        anchorMax.x = 1.0f;
                        break;
                }

                switch (y)
                {
                    case AnchorYType.Top:
                        anchorMin.y = 1.0f;
                        anchorMax.y = 1.0f;
                        break;
                    case AnchorYType.Middle:
                        anchorMin.y = 0.5f;
                        anchorMax.y = 0.5f;
                        break;
                    case AnchorYType.Bottom:
                        anchorMin.y = 0.0f;
                        anchorMax.y = 0.0f;
                        break;
                    case AnchorYType.Stretch:
                        anchorMin.y = 0.0f;
                        anchorMax.y = 1.0f;
                        break;
                }

                return (anchorMin, anchorMax);
            }

            var element = layoutInfo.Elements.Single(x => x.Eid == eid);

            if (element is ObjectElement objectElement)
            {
                var gameObject = new GameObject(objectElement.Name);
                gameObject.transform.SetParent(parent);

                var rectTransform = gameObject.AddComponent<RectTransform>();
                var (anchorMin, anchorMax) = CalcAnchor(objectElement.AnchorX, objectElement.AnchorY);
                gameObject.SetActive(objectElement.Visible);
                rectTransform.anchoredPosition = new Vector2(objectElement.Position.x, -objectElement.Position.y);
                rectTransform.localRotation = Quaternion.AngleAxis(objectElement.Rotation, Vector3.back);
                var p = rectTransform.localPosition;
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.localPosition = p;
                rectTransform.SetSize(objectElement.Size);

                foreach (var child in objectElement.Children)
                {
                    CreateGameObject(assetLoader, layoutInfo, child, rectTransform, triggers);
                }

                var createdComponents = new List<Component>();
                foreach (var component in objectElement.Components)
                {
                    createdComponents.Add(CreateComponent(assetLoader, gameObject, component, triggers));
                }

                return gameObject;
            }

            Debug.LogError($"Unknown element type {element}");
            return null;
        }

        private static Component CreateComponent(IAssetLoader assetLoader, GameObject gameObject, IComponent component, IAkyuiGenerateTrigger[] triggers)
        {
            foreach (var trigger in triggers)
            {
                var result = trigger.CreateComponent(gameObject, component, assetLoader);
                if (result != null)
                {
                    foreach (var postprocessTrigger in triggers) postprocessTrigger.OnPostprocessComponent(gameObject, component);
                    return result;
                }
            }

            Debug.LogError($"Unknown component type {component}");
            return null;
        }
    }

    public static class AkyuiGeneratorExtensions
    {
        public static void SetSize(this RectTransform rectTransform, Vector2 size)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
    }
}