using System;
using System.Collections.Generic;
using System.Linq;

namespace AkyuiUnity.Editor
{
    public class AkyuiLogger
    {
        private readonly string _name;
        private readonly List<string> _categories = new List<string>();

        private string Header => $"{_name}{string.Join("", _categories.Select(x => $"/{x}"))} |";

        public AkyuiLogger(string name)
        {
            _name = name;
        }

        public IDisposable SetCategory(string category)
        {
            _categories.Add(category);
            return new DisposeCategory(this, category);
        }

        public void Log(string message)
        {
            UnityEngine.Debug.Log($"{Header} {message}");
        }

        public void Log(string message, params (string, object)[] values)
        {
            UnityEngine.Debug.Log($"{Header} {message} {string.Join(", ", values.Select(x => $"{x.Item1} = {x.Item2}"))}");
        }

        public void Warning(string message)
        {
            UnityEngine.Debug.LogWarning($"{Header} {message}");
        }

        public void Warning(string message, params (string, object)[] values)
        {
            UnityEngine.Debug.LogWarning($"{Header} {message} {string.Join(", ", values.Select(x => $"{x.Item1} = {x.Item2}"))}");
        }

        public void Error(string message)
        {
            UnityEngine.Debug.LogError($"{Header} {message}");
        }

        public void Error(string message, params (string, object)[] values)
        {
            UnityEngine.Debug.LogError($"{Header} {message} {string.Join(", ", values.Select(x => $"{x.Item1} = {x.Item2}"))}");
        }

        private class DisposeCategory : IDisposable
        {
            private readonly AkyuiLogger _parent;
            private readonly string _category;

            public DisposeCategory(AkyuiLogger parent, string category)
            {
                _parent = parent;
                _category = category;
            }

            public void Dispose()
            {
                _parent._categories.Remove(_category);
            }
        }
    }
}