using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    [CustomEditor(typeof(XdImportSettings))]
    public class XdImportSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop xd", new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter });

            var e = Event.current;

            if (e.type == EventType.DragUpdated && dropArea.Contains(e.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }

            if (e.type == EventType.DragPerform && dropArea.Contains(e.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                DragAndDrop.activeControlID = 0;

                var settings = (XdImportSettings) target;
                var paths = DragAndDrop.paths.Where(x => Path.GetExtension(x) == ".xd").ToArray();
                if (paths.Length > 0) XdImporter.Import(settings, paths);
                Event.current.Use();
            }
        }
    }
}