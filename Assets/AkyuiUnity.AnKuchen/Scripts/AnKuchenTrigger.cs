using AkyuiUnity.Editor.ScriptableObject;
using AnKuchen.Map;
using UnityEngine;

namespace AkyuiUnity.AnKuchen
{
    [CreateAssetMenu(menuName = "Akyui/AnKuchenTrigger")]
    public class AnKuchenTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(GameObject prefab)
        {
            var uiCache = prefab.AddComponent<UICache>();
            uiCache.CreateCache();
        }
    }
}