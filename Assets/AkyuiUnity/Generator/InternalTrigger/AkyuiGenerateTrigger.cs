using UnityEngine;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public interface IAkyuiGenerateTrigger
    {
        Component SetOrCreateComponentValue(GameObject gameObject, TargetComponentGetter componentGetter, IComponent component, IAssetLoader assetLoader);
    }
}