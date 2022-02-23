using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    public class MessageChannel<T> : IMessageChannel<T>
    {
        private readonly List<Action<T>> m_MessageHandlers = new List<Action<T>>();

        /// <summary>
        /// This queue of actions that would either add or remove subscriber is used to prevent problems from immediate modification
        /// of the list of subscribers. It could happen if one decides to unsubscribe in a message handler etc.
        /// </summary>
        private readonly Queue<Action> m_PendingHandlers = new Queue<Action>();

        public bool IsDisposed { get; private set; } = false;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                m_MessageHandlers.Clear();
                m_PendingHandlers.Clear();
            }
        }

        public virtual void Publish(T message)
        {
            while (m_PendingHandlers.Count > 0)
            {
                m_PendingHandlers.Dequeue()?.Invoke();
            }

            foreach (var messageHandler in m_MessageHandlers)
            {
                messageHandler?.Invoke(message);
            }
        }

        public virtual IDisposable Subscribe(Action<T> handler)
        {
            Assert.IsTrue(!m_MessageHandlers.Contains(handler), "Attempting to subscribe with the same handler more than once");
            m_PendingHandlers.Enqueue(() => { DoSubscribe(handler); });
            var subscription = new DisposableSubscription<T>(this, handler);
            return subscription;

            void DoSubscribe(Action<T> _h)
            {
                if (_h != null && !m_MessageHandlers.Contains(_h))
                {
                    m_MessageHandlers.Add(_h);
                }
            }
        }

        public void Unsubscribe(Action<T> handler)
        {
            m_PendingHandlers.Enqueue(() => { DoUnsubscribe(handler); });

            void DoUnsubscribe(Action<T> _h)
            {
                m_MessageHandlers.Remove(_h);
            }
        }
    }
}