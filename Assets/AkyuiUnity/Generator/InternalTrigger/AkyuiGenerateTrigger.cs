using UnityEngine;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public interface IAkyuiGenerateTrigger
    {
        Component SetOrCreateComponentValue(Component target, IAssetLoader assetLoader, GameObject gameObject, IComponent component);
    }
}