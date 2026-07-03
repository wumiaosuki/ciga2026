using System;
using System.Collections.Generic;

namespace Ciga2026.Framework.Events
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> listenersByType = new();

        public static EventBus Global { get; } = new EventBus();

        public void Subscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var eventType = typeof(TEvent);

            if (!listenersByType.TryGetValue(eventType, out var listeners))
            {
                listeners = new List<Delegate>();
                listenersByType[eventType] = listeners;
            }

            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                return;
            }

            var eventType = typeof(TEvent);

            if (!listenersByType.TryGetValue(eventType, out var listeners))
            {
                return;
            }

            listeners.Remove(listener);

            if (listeners.Count == 0)
            {
                listenersByType.Remove(eventType);
            }
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var eventType = typeof(TEvent);

            if (!listenersByType.TryGetValue(eventType, out var listeners))
            {
                return;
            }

            var snapshot = listeners.ToArray();

            for (var i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] is Action<TEvent> listener)
                {
                    listener.Invoke(eventData);
                }
            }
        }

        public void Clear<TEvent>()
        {
            listenersByType.Remove(typeof(TEvent));
        }

        public void ClearAll()
        {
            listenersByType.Clear();
        }

        public int GetListenerCount<TEvent>()
        {
            return listenersByType.TryGetValue(typeof(TEvent), out var listeners) ? listeners.Count : 0;
        }
    }
}
