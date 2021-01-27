using System;
using UnityEditor;

namespace AkyuiUnity.Editor
{
    public interface IAkyuiProgress : IDisposable
    {
        void SetTotal(int total);
        void IncrementCurrent();

        IAkyuiProgress TaskStart(string message);
        void Update(string message, float progress);
    }

    public class AkyuiProgressBar : IAkyuiProgress
    {
        private readonly string _title;
        private int _current;
        private int _total;

        public AkyuiProgressBar(string title)
        {
            _title = title;
            EditorUtility.DisplayProgressBar(_title, "initializing...", 0f);
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }

        public void SetTotal(int total)
        {
            _current = 0;
            _total = total;
        }

        public void IncrementCurrent()
        {
            _current++;
        }

        public IAkyuiProgress TaskStart(string message)
        {
            return new AkyuiProgressBarTask(this, message);
        }

        public void Update(string message, float progress)
        {
            // UnityEngine.Debug.Log($@"EditorUtility.DisplayProgressBar(_title, ""[{_current + 1}/{_total}] {message}"", {(_current + progress) / _total})");
            EditorUtility.DisplayProgressBar(_title, $"[{_current + 1}/{_total}] {message}", (_current + progress) / _total);
        }

        public class AkyuiProgressBarTask : IAkyuiProgress
        {
            private readonly IAkyuiProgress _parent;
            private readonly string _message;
            private bool _disposed;

            private int _current;
            private int _total;

            public AkyuiProgressBarTask(IAkyuiProgress parent, string message)
            {
                _parent = parent;
                _message = message;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                _parent.Update(_message, 1f);
                _parent.IncrementCurrent();
            }

            public void SetTotal(int total)
            {
                _current = 0;
                _total = total;
            }

            public void IncrementCurrent()
            {
                _current++;
            }

            public IAkyuiProgress TaskStart(string message)
            {
                return new AkyuiProgressBarTask(this, message);
            }

            public void Update(string message, float progress)
            {
                _parent.Update($"{_message} | [{_current + 1}/{_total}] {message}", (_current + progress) / _total);
            }
        }
    }
}