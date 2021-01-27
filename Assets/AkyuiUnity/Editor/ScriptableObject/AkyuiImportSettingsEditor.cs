using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    [CustomEditor(typeof(AkyuiImportSettings))]
    public class AkyuiImportSettingsEditor : UnityEditor.Editor
    {
        private readonly HistoryHolder _historyHolder = new HistoryHolder("Akyui.History");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(20);

            var dropArea = GUILayoutUtility.GetRect(0.0f, 80.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop aky", new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter });

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

                var settings = (AkyuiImportSettings) target;
                var paths = DragAndDrop.paths.Where(x => Path.GetExtension(x) == ".aky").ToArray();
                if (paths.Length > 0) Importer.Import(settings, paths);
                Event.current.Use();
                _historyHolder.Save(paths);
            }

            EditorGUILayout.Space(20);

            _historyHolder.OnGui(x =>
            {
                var settings = (AkyuiImportSettings) target;
                Importer.Import(settings, new[] { x });
            });
        }
    }

    public class HistoryHolder
    {
        private readonly string _playerPrefsKey;

        public HistoryHolder(string playerPrefsKey)
        {
            _playerPrefsKey = playerPrefsKey;
        }

        private string[] _histories;

        private string[] Histories
        {
            get
            {
                if (_histories == null)
                {
                    _histories = EditorPrefs.GetString(_playerPrefsKey, string.Empty)
                        .Split(',')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                }
                return _histories;
            }

            set
            {
                _histories = value;
                EditorPrefs.SetString(_playerPrefsKey, string.Join(",", _histories));
            }
        }

        public void Save(string[] paths)
        {
            var histories = Histories.ToList();
            foreach (var path in paths)
            {
                histories.Remove(path);
                histories.Insert(0, path);
            }
            Histories = histories.Take(10).ToArray();
        }

        public void Remove(string[] paths)
        {
            var histories = Histories.ToList();
            foreach (var path in paths)
            {
                histories.Remove(path);
            }
            Histories = histories.Take(10).ToArray();
        }

        public void OnGui(Action<string> onClick)
        {
            var histories = Histories;
            if (histories.Length > 0)
            {
                EditorGUILayout.LabelField("History");

                foreach (var history in histories)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(history))
                    {
                        onClick(history);
                        break;
                    }

                    if (GUILayout.Button("x", GUILayout.Width(30)))
                    {
                        Remove(new[] { history });
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}