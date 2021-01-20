using System.Linq;
using AkyuiUnity.Editor.ScriptableObject;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/SetSpriteMaxSize", fileName = nameof(SetSpriteMaxSizeTrigger))]
    public class SetSpriteMaxSizeTrigger : AkyuiImportTrigger
    {
        [SerializeField] private int maxTextureSize = 512;

        public override void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset)
        {
            if (assetImporter is TextureImporter textureImporter)
            {
                textureImporter.maxTextureSize = maxTextureSize;
            }
        }
    }
}