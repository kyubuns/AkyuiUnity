using System.IO;
using AkyuiUnity.Editor;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.Extensions
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/GitKeep", fileName = nameof(GitKeepTrigger))]
    public class GitKeepTrigger : AkyuiImportTrigger
    {
        public override void OnPostprocessFile(IAkyuiLoader loader, IPathGetter pathGetter)
        {
            var gitKeepPath = Path.Combine(pathGetter.AssetOutputDirectoryPath, ".gitkeep");
            if (File.Exists(gitKeepPath)) return;

            File.Create(gitKeepPath).Close();
        }
    }
}
