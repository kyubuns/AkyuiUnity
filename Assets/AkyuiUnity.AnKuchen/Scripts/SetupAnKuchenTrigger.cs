using AkyuiUnity.Editor.ScriptableObject;
using AnKuchen.Map;
using UnityEngine;

namespace AkyuiUnity.AnKuchen
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/SetupAnKuchen", fileName = nameof(SetupAnKuchenTrigger))]
    public class SetupAnKuchenTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(ref GameObject prefab, ref GameObjectWithId[] meta)
        {
            var uiCache = prefab.AddComponent<UICache>();
            uiCache.CreateCache();
        }
    }
}