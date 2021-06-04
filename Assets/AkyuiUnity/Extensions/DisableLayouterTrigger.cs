using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Extensions
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/DisableLayouter", fileName = nameof(DisableLayouterTrigger))]
    public class DisableLayouterTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab)
        {
            foreach (var layoutGroup in prefab.GetComponentsInChildren<LayoutGroup>(true))
            {
                layoutGroup.enabled = false;
            }
        }
    }
}