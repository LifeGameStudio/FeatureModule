namespace GameModule.Observer.Scripts
{
    using System;
    using System.Collections.Generic;
    using Zenject;

    public class Observer : ISignalBus
    {
        // Dictionary to store signal callbacks
        private readonly Dictionary<Type, List<Delegate>> _subscriptions = new Dictionary<Type, List<Delegate>>();

        public void Fire<TSignal>()
        {
            var signalType = typeof(TSignal);

            if (!this._subscriptions.TryGetValue(signalType, out var delegates)) return;

            foreach (var callback in delegates)
            {
                if (callback is Action action)
                {
                    action.Invoke();
                }
            }
        }

        public void Fire<TSignal>(TSignal signal)
        {
            var signalType = typeof(TSignal);

            if (!this._subscriptions.TryGetValue(signalType, out var delegates)) return;

            foreach (var callback in delegates)
            {
                if (callback is Action<TSignal> action)
                {
                    action.Invoke(signal);
                }
            }
        }

        public void Subscribe<TSignal>(Action callback)
        {
            var signalType = typeof(TSignal);
            if (!this._subscriptions.ContainsKey(signalType))
            {
                this._subscriptions[signalType] = new List<Delegate>();
            }

            this._subscriptions[signalType].Add(callback);
        }

        public void Subscribe<TSignal>(Action<TSignal> callback)
        {
            var signalType = typeof(TSignal);
            if (!this._subscriptions.ContainsKey(signalType))
            {
                this._subscriptions[signalType] = new List<Delegate>();
            }

            this._subscriptions[signalType].Add(callback);
        }

        public bool TrySubscribe<TSignal>(Action callback)
        {
            try
            {
                this.Subscribe<TSignal>(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TrySubscribe<TSignal>(Action<TSignal> callback)
        {
            try
            {
                this.Subscribe<TSignal>(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Unsubscribe<TSignal>(Action callback)
        {
            var signalType = typeof(TSignal);

            if (!this._subscriptions.TryGetValue(signalType, out var delegates)) return;
            delegates.Remove(callback);
            if (delegates.Count == 0)
            {
                this._subscriptions.Remove(signalType);
            }
        }

        public void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            var signalType = typeof(TSignal);

            if (!this._subscriptions.TryGetValue(signalType, out var delegates)) return;
            delegates.Remove(callback);
            if (delegates.Count == 0)
            {
                this._subscriptions.Remove(signalType);
            }
        }

        public bool TryUnsubscribe<TSignal>(Action callback)
        {
            try
            {
                Unsubscribe<TSignal>(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryUnsubscribe<TSignal>(Action<TSignal> callback)
        {
            try
            {
                Unsubscribe<TSignal>(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
