using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Generator.InternalTrigger;
using JetBrains.Annotations;
using UnityEngine;

namespace AkyuiUnity.Generator
{
    public static class AkyuiGenerator
    {
        public static (GameObject, AkyuiPrefabMeta) GenerateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo, IAkyuiGenerateTrigger[] triggers)
        {
            var metaList = new List<GameObjectWithId>();
            var triggersWithDefault = triggers.Concat(new[] { new DefaultGenerateTrigger() }).ToArray();
            var gameObject = CreateGameObject(assetLoader, layoutInfo, layoutInfo.Root, null, triggersWithDefault, ref metaList);
            var meta = new AkyuiPrefabMeta
            {
                hash = layoutInfo.Hash,
                root = gameObject,
                idAndGameObjects = metaList.ToArray()
            };
            return (gameObject, meta);
        }

        private static GameObject CreateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo, int eid, Transform parent, IAkyuiGenerateTrigger[] triggers, ref List<GameObjectWithId> meta)
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
                rectTransform.anchoredPosition = objectElement.Position;
                var p = rectTransform.localPosition;
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.localPosition = p;
                rectTransform.SetSize(objectElement.Size);

                var createdComponents = new List<ComponentWithId>();
                foreach (var component in objectElement.Components)
                {
                    createdComponents.Add(CreateComponent(assetLoader, gameObject, component, triggers));
                }

                meta.Add(new GameObjectWithId
                {
                    eid = new[] { objectElement.Eid },
                    gameObject = gameObject,
                    idAndComponents = createdComponents.ToArray(),
                });

                foreach (var child in objectElement.Children)
                {
                    CreateGameObject(assetLoader, layoutInfo, child, rectTransform, triggers, ref meta);
                }

                return gameObject;
            }

            if (element is PrefabElement prefabElement)
            {
                var (prefabGameObject, referenceMeta) = assetLoader.LoadPrefab(parent, prefabElement.Reference);

                if (prefabElement.Hash != referenceMeta.hash)
                {
                    Debug.LogWarning($"Reference {prefabElement.Reference} hash mismatch {prefabElement.Hash} != {referenceMeta.hash}");
                }

                {
                    var rectTransform = prefabGameObject.GetComponent<RectTransform>();
                    var (anchorMin, anchorMax) = CalcAnchor(prefabElement.AnchorX, prefabElement.AnchorY);
                    rectTransform.anchoredPosition = prefabElement.Position;
                    var p = rectTransform.localPosition;
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.localPosition = p;
                    rectTransform.SetSize(prefabElement.Size);
                }

                foreach (var @override in prefabElement.Overrides)
                {
                    var targetObject = referenceMeta.Find(@override.Eid);
                    var rectTransform = targetObject.gameObject.GetComponent<RectTransform>();

                    if (@override.Name != null) targetObject.gameObject.name = @override.Name;
                    if (@override.Position != null) rectTransform.anchoredPosition = @override.Position.Value;
                    if (@override.Size != null) rectTransform.sizeDelta = @override.Size.Value;

                    var anchorMin = rectTransform.anchorMin;
                    var anchorMax = rectTransform.anchorMax;

                    if (@override.AnchorX != null)
                    {
                        switch (@override.AnchorX.Value)
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
                    }

                    if (@override.AnchorY != null)
                    {
                        switch (@override.AnchorY.Value)
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
                    }

                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;

                    if (@override.Components != null)
                    {
                        foreach (var component in @override.Components)
                        {
                            var targetComponent = targetObject.idAndComponents.Single(x => x.cid == component.Cid);
                            SetOrCreateComponentValue(targetComponent.component, assetLoader, targetObject.gameObject, component, triggers);
                        }
                    }
                }

                foreach (var idAndGameObject in referenceMeta.idAndGameObjects)
                {
                    meta.Add(new GameObjectWithId
                    {
                        eid = new[] { prefabElement.Eid }.Concat(idAndGameObject.eid).ToArray(),
                        gameObject = idAndGameObject.gameObject,
                        idAndComponents = idAndGameObject.idAndComponents
                    });
                }

                return prefabGameObject;
            }

            Debug.LogError($"Unknown element type {element}");
            return null;
        }

        private static ComponentWithId CreateComponent(IAssetLoader assetLoader, GameObject gameObject, IComponent component, IAkyuiGenerateTrigger[] triggers)
        {
            return new ComponentWithId { cid = component.Cid, component = SetOrCreateComponentValue(null, assetLoader, gameObject, component, triggers) };
        }

        private static Component SetOrCreateComponentValue([CanBeNull] Component target, IAssetLoader assetLoader, GameObject gameObject, IComponent component, IAkyuiGenerateTrigger[] triggers)
        {
            foreach (var trigger in triggers)
            {
                var result = trigger.SetOrCreateComponentValue(target, assetLoader, gameObject, component);
                if (result != null) return result;
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