using System.Linq;
using AkyuiUnity.Editor.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Sample
{
    [CreateAssetMenu(menuName = "AkyuiSample/ButtonPostProcessTrigger")]
    public class ButtonPostProcessTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(ref GameObject prefab, ref GameObjectWithId[] idAndGameObjects)
        {
            foreach (var button in prefab.GetComponentsInChildren<Button>())
            {
                var image = button.GetComponentsInChildren<Image>()
                    .Where(x => x.color != Color.clear)
                    .ToArray();

                if (image.Length == 1)
                {
                    button.targetGraphic = image.First();
                }
            }
        }
    }
}
