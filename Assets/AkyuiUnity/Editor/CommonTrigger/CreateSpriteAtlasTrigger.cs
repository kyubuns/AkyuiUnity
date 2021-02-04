using System.IO;
using AkyuiUnity.Editor;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/CreateSpriteAtlas", fileName = nameof(CreateSpriteAtlasTrigger))]
    public class CreateSpriteAtlasTrigger : AkyuiImportTrigger
    {
        [SerializeField] private string spriteAtlasOutputPath = "Assets/{name}SpriteAtlas";
        [SerializeField] private SpriteAtlas source = default;

        public override void OnPostprocessAllAssets(IAkyuiLoader loader, Object[] importAssets)
        {
            var spriteAtlasPath = spriteAtlasOutputPath.Replace("{name}", loader.LayoutInfo.Name) + ".spriteatlas";
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(spriteAtlasPath);
            if (spriteAtlas == null)
            {
                Debug.Log($"Create SpriteAtlas: {spriteAtlasPath}");

                AkyuiEditorUtil.CreateDirectory(Path.GetDirectoryName(spriteAtlasPath));
                spriteAtlas = new SpriteAtlas();
                spriteAtlas.name = loader.LayoutInfo.Name;
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            }

            if (source != null)
            {
                EditorUtility.CopySerialized(source, spriteAtlas);
            }

            spriteAtlas.Remove(spriteAtlas.GetPackables());
            spriteAtlas.Add(importAssets);

            SpriteAtlasUtility.PackAtlases(new[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
        }
    }
}