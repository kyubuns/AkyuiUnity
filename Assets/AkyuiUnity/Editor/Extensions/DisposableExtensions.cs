using System;

namespace AkyuiUnity.Editor.Extensions
{
    public static class Disposable
    {
        public static IDisposable Create(Action action) => new DisposableEvent(action);
    }

    public class DisposableEvent : IDisposable
    {
        private readonly Action _action;

        public DisposableEvent(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action.Invoke();
        }
    }
}