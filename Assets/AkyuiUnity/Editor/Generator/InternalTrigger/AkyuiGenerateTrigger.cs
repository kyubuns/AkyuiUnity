using UnityEngine;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public interface IAkyuiGenerateTrigger
    {
        Component CreateComponent(GameObject gameObject, IComponent component, IAssetLoader assetLoader);
        void OnPostprocessComponent(GameObject gameObject, IComponent component);
    }
}