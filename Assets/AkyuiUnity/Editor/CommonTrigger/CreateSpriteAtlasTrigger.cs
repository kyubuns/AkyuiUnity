using System.IO;
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

        public override void OnPostprocessAllAssets(IAkyuiLoader loader, string outputDirectoryPath, Object[] importAssets)
        {
            var tmpPath = outputDirectoryPath.TrimEnd('/');
            var fileName = Path.GetFileName(tmpPath);

            var spriteAtlasPath = spriteAtlasOutputPath.Replace("{name}", fileName) + ".spriteatlas";
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(spriteAtlasPath);
            if (spriteAtlas == null)
            {
                Debug.Log($"Create SpriteAtlas: {spriteAtlasPath}");

                spriteAtlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            }

            var spriteDirectory = AssetDatabase.LoadAssetAtPath<Object>(tmpPath);
            spriteAtlas.Remove(new[] { spriteDirectory });
            spriteAtlas.Add(new[] { spriteDirectory });
        }
    }
}