using System.Linq;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/SetButtonTargetGraphic", fileName = nameof(SetButtonTargetGraphicTrigger))]
    public class SetButtonTargetGraphicTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab)
        {
            foreach (var button in prefab.GetComponentsInChildren<Button>())
            {
                var image = button.GetComponentsInChildren<Image>()
                    .Where(x => x.color != Color.clear)
                    .ToArray();

                if (image.Length >= 1)
                {
                    button.targetGraphic = image.First();
                }

                DestroyImmediate(button.GetComponent<Image>());
            }
        }
    }
}
