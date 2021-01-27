using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.Sample
{
    [CreateAssetMenu(menuName = "AkyuiSample/DebugLogTrigger")]
    public class DebugLogTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab)
        {
            Debug.Log($"OnPostprocessPrefab: {prefab}");
        }
    }
}