using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    public abstract class AkyuiImportTrigger : UnityEngine.ScriptableObject
    {
        public virtual void OnPostprocessPrefab(GameObject prefab)
        {
        }
    }
}