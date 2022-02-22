using System;
using System.Collections.Generic;
using System.Linq;

namespace AkyuiUnity.Editor
{
    public class AkyuiLogger
    {
        private readonly string _name;
        private readonly AkyuiLogType _logType;
        private readonly List<string> _categories = new List<string>();

        private string Header => $"{_name}{string.Join("", _categories.Select(x => $"/{x}"))} |";

        public AkyuiLogger(string name, AkyuiLogType logType)
        {
            _name = name;
            _logType = logType;
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
            var s = $"{Header} {message}";
            if (_logType == AkyuiLogType.WarningAsException) throw new AkyuiImportException(s);
            if (_logType == AkyuiLogType.WarningAsLogError) UnityEngine.Debug.LogError(s);
            else UnityEngine.Debug.LogWarning(s);
        }

        public void Warning(string message, params (string, object)[] values)
        {
            var s = $"{Header} {message} {string.Join(", ", values.Select(x => $"{x.Item1} = {x.Item2}"))}";
            if (_logType == AkyuiLogType.WarningAsException) throw new AkyuiImportException(s);
            if (_logType == AkyuiLogType.WarningAsLogError) UnityEngine.Debug.LogError(s);
            else UnityEngine.Debug.LogWarning(s);
        }

        public void Error(string message)
        {
            var s = $"{Header} {message}";
            if (_logType == AkyuiLogType.WarningAsException) throw new AkyuiImportException(s);
            UnityEngine.Debug.LogError(s);
        }

        public void Error(string message, params (string, object)[] values)
        {
            var s = $"{Header} {message} {string.Join(", ", values.Select(x => $"{x.Item1} = {x.Item2}"))}";
            if (_logType == AkyuiLogType.WarningAsException) throw new AkyuiImportException(s);
            UnityEngine.Debug.LogError(s);
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

    public enum AkyuiLogType
    {
        Default,
        WarningAsLogError,
        WarningAsException,
    }

    public class AkyuiImportException : Exception
    {
        public AkyuiImportException(string message) : base(message)
        {
        }
    }
}
