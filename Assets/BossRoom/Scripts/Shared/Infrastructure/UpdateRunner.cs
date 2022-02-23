﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    /// <summary>
    /// Some objects might need to be on a slower update loop than the usual MonoBehaviour Update and without precise timing, e.g. to refresh data from services.
    /// Some might also not want to be coupled to a Unity object at all but still need an update loop.
    /// </summary>
    public class UpdateRunner : MonoBehaviour
    {
        private class SubscriberData
        {
            public float Period;
            public float PeriodCurrent;
        }

        private readonly Queue<Action> m_PendingHandlers = new Queue<Action>();
        private readonly HashSet<Action<float>> m_Subscribers = new HashSet<Action<float>>();
        private readonly Dictionary<Action<float>, SubscriberData> m_SubscriberData = new Dictionary<Action<float>, SubscriberData>();

        public void OnDestroy()
        {
            m_Subscribers.Clear(); // We should clean up references in case they would prevent garbage collection.
        }

        /// <summary>
        /// Subscribe in order to have onUpdate called approximately every period seconds (or every frame, if period <= 0).
        /// Don't assume that onUpdate will be called in any particular order compared to other subscribers.
        /// </summary>
        public void Subscribe(Action<float> onUpdate, float period)
        {
            if (onUpdate == null)
            {
                return;
            }

            if (onUpdate.Target == null) // Detect a local function that cannot be Unsubscribed since it could go out of scope.
            {
                Debug.LogError("Can't subscribe to a local function that can go out of scope and can't be unsubscribed from");
                return;
            }

            if (onUpdate.Method.ToString().Contains("<")) // Detect
            {
                Debug.LogError("Can't subscribe with an anonymous function that cannot be Unsubscribed, by checking for a character that can't exist in a declared method name.");
                return;
            }

            if (!m_Subscribers.Contains(onUpdate))
            {
                m_PendingHandlers.Enqueue(() =>
                {
                    m_Subscribers.Add(onUpdate);
                    m_SubscriberData.Add(onUpdate, new SubscriberData(){Period = period, PeriodCurrent = 0});
                });
            }
        }

        /// <summary>
        /// Safe to call even if onUpdate was not previously Subscribed.
        /// </summary>
        public void Unsubscribe(Action<float> onUpdate)
        {
            m_PendingHandlers.Enqueue(() =>
            {
                m_Subscribers.Remove(onUpdate);
                m_SubscriberData.Remove(onUpdate);
            } );
        }

        /// <summary>
        /// Each frame, advance all subscribers. Any that have hit their period should then act, though if they take too long they could be removed.
        /// </summary>
        private void Update()
        {
            while (m_PendingHandlers.Count > 0)
            {
                m_PendingHandlers.Dequeue()?.Invoke();
            }

            float dt = Time.deltaTime;

            foreach (var subscriber in m_Subscribers)
            {
                var subscriberData = m_SubscriberData[subscriber];
                subscriberData.PeriodCurrent += dt;

                if (subscriberData.PeriodCurrent > subscriberData.Period)
                {
                    subscriber.Invoke(subscriberData.PeriodCurrent);
                    subscriberData.PeriodCurrent = 0;
                }
            }
        }
    }
}