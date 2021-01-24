#if AKYUIUNITY_ANKUCHEN_SUPPORT
using System.Collections.Generic;
using AkyuiUnity.Editor.ScriptableObject;
using AnKuchen.AdditionalInfo;
using AnKuchen.Map;
using UnityEngine;

namespace AkyuiUnity.AnKuchenExtension
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/SetupAnKuchen", fileName = nameof(SetupAnKuchenTrigger))]
    public class SetupAnKuchenTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessPrefab(ref GameObject prefab, ref GameObjectWithId[] meta)
        {
            var uiCache = prefab.AddComponent<UICache>();
            uiCache.CreateCache();
        }

        public override void OnPostprocessComponent(GameObject gameObject, IComponent component)
        {
            if (component is VerticalListComponent verticalListComponent)
            {
                if (gameObject.GetComponent<ListAdditionalInfo>() != null) return;

                var additionalInfo = gameObject.AddComponent<ListAdditionalInfo>();

                var spacialSpacings = new List<AnKuchen.AdditionalInfo.SpecialSpacing>();
                if (verticalListComponent.SpacialSpacings != null)
                {
                    foreach (var a in verticalListComponent.SpacialSpacings)
                    {
                        spacialSpacings.Add(new AnKuchen.AdditionalInfo.SpecialSpacing
                        {
                            item1 = a.Item1,
                            item2 = a.Item2,
                            spacing = a.Spacing,
                        });
                    }
                }

                additionalInfo.specialSpacings = spacialSpacings.ToArray();
            }
        }
    }
}
#endif
