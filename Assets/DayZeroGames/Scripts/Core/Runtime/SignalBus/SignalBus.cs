using System;
using System.Collections.Generic;
using DZ.Core.Contracts;

namespace DZ.Core.Runtime
{
    public class SignalBus : ISignalBus
    {
        private readonly Dictionary<Type, Delegate> _listeners = new();

        public void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            if(_listeners.TryGetValue(type, out var existingDelegate)) (existingDelegate as Action<T>)?.Invoke(eventData);
        }

        public void Subscribe<T>(Action<T> listener) where T : struct
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            var type = typeof(T);
            if (_listeners.TryGetValue(type, out var existingDelegate))
                _listeners[type] = Delegate.Combine(existingDelegate, listener);
            else _listeners[type] = listener;
        }

        public void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            var type = typeof(T);
            if (!_listeners.TryGetValue(type, out var existingDelegate)) return;
            var currentDelegate = Delegate.Remove(existingDelegate, listener);
            if (currentDelegate == null) _listeners.Remove(type);
            else _listeners[type] = currentDelegate;
        }
    }
}
