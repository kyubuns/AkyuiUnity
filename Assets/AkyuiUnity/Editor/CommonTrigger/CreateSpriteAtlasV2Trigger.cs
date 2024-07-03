using System.IO;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.U2D;
using UnityEngine;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/CreateSpriteAtlasV2", fileName = nameof(CreateSpriteAtlasV2Trigger))]
    public class CreateSpriteAtlasV2Trigger : AkyuiImportTrigger
    {
        [SerializeField] private string spriteAtlasOutputPath = "Assets/{name}SpriteAtlas";
        [SerializeField] private Preset spriteAtlasPreset = default;

        public override void OnPostprocessAllAssets(IAkyuiLoader loader, Object[] importAssets)
        {
            var spriteAtlasPath = spriteAtlasOutputPath.Replace("{name}", loader.LayoutInfo.Name) + ".spriteatlasv2";
            var spriteAtlas = new SpriteAtlasAsset
            {
                name = Path.GetFileNameWithoutExtension(spriteAtlasPath)
            };

            spriteAtlas.Add(importAssets);
            SpriteAtlasAsset.Save(spriteAtlas, spriteAtlasPath);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(spriteAtlasPath);
            if (spriteAtlasPreset != null)
            {
                spriteAtlasPreset.ApplyTo(importer);
                importer.SaveAndReimport();
            }
        }
    }
}
