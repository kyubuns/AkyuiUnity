using System.IO;
using AkyuiUnity.Editor;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/CreateSpriteAtlas", fileName = nameof(CreateSpriteAtlasTrigger))]
    public class CreateSpriteAtlasTrigger : AkyuiImportTrigger
    {
        [SerializeField] private string spriteAtlasOutputPath = "Assets/{name}SpriteAtlas";
        [SerializeField] private Preset spriteAtlasPreset = default;

        public override void OnPostprocessAllAssets(IAkyuiLoader loader, Object[] importAssets)
        {
            var spriteAtlasPath = spriteAtlasOutputPath.Replace("{name}", loader.LayoutInfo.Name) + ".spriteatlas";
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(spriteAtlasPath);
            if (spriteAtlas == null)
            {
                Debug.Log($"Create SpriteAtlas: {spriteAtlasPath}");

                AkyuiEditorUtil.CreateDirectory(Path.GetDirectoryName(spriteAtlasPath));
                spriteAtlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            }

            if (spriteAtlasPreset != null)
            {
                spriteAtlasPreset.ApplyTo(spriteAtlas);
            }

            spriteAtlas.name = Path.GetFileNameWithoutExtension(spriteAtlasPath);
            spriteAtlas.Remove(spriteAtlas.GetPackables());
            spriteAtlas.Add(importAssets);

            SpriteAtlasUtility.PackAtlases(new[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);

            // なにかのバグだと思うんだけど、ここでSaveしないとPresetが反映されないことがある Unity 2020.3.18
            EditorUtility.SetDirty(spriteAtlas);
            AssetDatabase.SaveAssets();
        }
    }
}