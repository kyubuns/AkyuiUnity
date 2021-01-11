using AkyuiUnity.Editor.ScriptableObject;
using UnityEngine;

namespace AkyuiUnity.Sample
{
    [CreateAssetMenu(menuName = "AkyuiSample/DebugLogTrigger")]
    public class DebugLogTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(GameObject prefab, GameObjectWithId[] idAndGameObjects)
        {
            Debug.Log($"OnPostprocessPrefab: {prefab}");
        }
    }
}